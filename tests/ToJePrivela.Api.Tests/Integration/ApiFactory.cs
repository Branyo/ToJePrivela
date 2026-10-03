using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Identity.Tokens;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Api.Tests.Integration;

/// <summary>
/// Hosts the real API on a throwaway SQLite file with the AI and sign-in providers stubbed out. Clients from
/// <see cref="WebApplicationFactory{TEntryPoint}.CreateClient()"/> are signed in as <see cref="Admin"/>.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"tojeprivela-tests-{Guid.NewGuid():N}.db");
    private int _generatedCount;

    public const string GoogleClientId = "test-google-client";

    public ApiFactory()
    {
        ReplyWithFreshQuestions();
        GoogleVerifier.Provider.Returns(IdentityProvider.Google);
        GoogleVerifier.ClientId.Returns(GoogleClientId);
        FacebookVerifier.Provider.Returns(IdentityProvider.Facebook);
        FacebookVerifier.ClientId.Returns((string?)null);
    }

    public IQuestionGenerator QuestionGenerator { get; } = Substitute.For<IQuestionGenerator>();

    /// <summary>Configured: answers whatever a test tells it to.</summary>
    public IExternalIdentityVerifier GoogleVerifier { get; } = Substitute.For<IExternalIdentityVerifier>();

    /// <summary>Not configured (no client id), so Facebook sign-in is not offered.</summary>
    public IExternalIdentityVerifier FacebookVerifier { get; } = Substitute.For<IExternalIdentityVerifier>();

    /// <summary>A signed-in admin; every client signs in as this account unless a test asks for another.</summary>
    public Account Admin { get; private set; } = default!;

    /// <summary>A signed-in account without admin rights.</summary>
    public Account Member { get; private set; } = default!;

    public HttpClient CreateClientAs(Account account)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(account));
        return client;
    }

    public HttpClient CreateAnonymousClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = null;
        return client;
    }

    public string TokenFor(Account account) => Services.GetRequiredService<IAccessTokenIssuer>().Issue(account).Value;

    /// <summary>High enough that ordinary tests never hit the AI rate limit.</summary>
    protected virtual int AiGenerationPermitLimit => 10_000;

    /// <summary>Every call answers with as many new, unique questions as it was asked for.</summary>
    public void ReplyWithFreshQuestions()
    {
        QuestionGenerator
            .GenerateAsync(Arg.Any<QuestionGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => Fresh(call.Arg<QuestionGenerationRequest>().Count));

        QuestionGenerator
            .GenerateSubtopicsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);
    }

    public void ReplyWithNothing() =>
        QuestionGenerator
            .GenerateAsync(Arg.Any<QuestionGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns([]);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            $"ConnectionStrings:{SqliteConnectionString.Name}",
            $"Data Source={_databasePath}");

        builder.UseSetting($"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)}", "integration-tests-signing-key-0123456789");

        builder.UseSetting(
            $"{AiRateLimitOptions.SectionName}:{nameof(AiRateLimitOptions.PermitLimit)}",
            AiGenerationPermitLimit.ToString());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IQuestionGenerator>();
            services.AddScoped(_ => QuestionGenerator);
            services.RemoveAll<IExternalIdentityVerifier>();
            services.AddScoped(_ => GoogleVerifier);
            services.AddScoped(_ => FacebookVerifier);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ToJePrivelaDbContext>();
        context.Database.Migrate();

        var now = DateTime.UtcNow;
        Admin = Account.Register(IdentityProvider.Google, "test-admin", "admin@test.local", "Test admin", now);
        Admin.GrantAdmin();
        Member = Account.Register(IdentityProvider.Google, "test-member", "member@test.local", "Test member", now);
        context.Accounts.AddRange(Admin, Member);
        context.SaveChanges();

        return host;
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenFor(Admin));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    private IReadOnlyList<GeneratedQuestion> Fresh(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => Interlocked.Increment(ref _generatedCount))
            .Select(number => new GeneratedQuestion($"Generated test question number {number}?", (number + 1000).ToString()))
            .ToList();
}

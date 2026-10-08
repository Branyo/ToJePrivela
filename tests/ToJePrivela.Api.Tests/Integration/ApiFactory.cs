using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Identity.Tokens;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Api.Tests.Integration;

/// <summary>
/// Hosts the real API on a throwaway SQLite file with the AI provider stubbed out. Clients from
/// <see cref="WebApplicationFactory{TEntryPoint}.CreateClient()"/> are signed in as <see cref="Admin"/>.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"tojeprivela-tests-{Guid.NewGuid():N}.db");
    private int _generatedCount;

    public const string AdminPassword = "admin-password";
    public const string MemberPassword = "member-password";

    public ApiFactory()
    {
        ReplyWithFreshQuestions();
    }

    public IQuestionGenerator QuestionGenerator { get; } = Substitute.For<IQuestionGenerator>();

    /// <summary>Translates "Name" to "Name (En)" or "Name (Sk)".</summary>
    public ITextTranslator Translator { get; } = Substitute.For<ITextTranslator>();

    /// <summary>
    /// A signed-in admin (<see cref="AdminPassword"/>) owning the seeded players; every client signs in as this login
    /// unless a test asks for another.
    /// </summary>
    public Account Admin { get; private set; } = default!;

    /// <summary>A login without admin rights (<see cref="MemberPassword"/>).</summary>
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

    /// <summary>High enough that ordinary tests never hit the sign-in rate limit.</summary>
    protected virtual int SignInPermitLimit => 10_000;

    /// <summary>High enough that ordinary tests never hit the sign-in cap per address.</summary>
    protected virtual int SignInAddressPermitLimit => 10_000;

    /// <summary>Every call answers with as many new, unique questions as it was asked for.</summary>
    public void ReplyWithFreshQuestions()
    {
        QuestionGenerator
            .GenerateAsync(Arg.Any<QuestionGenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => Fresh(call.Arg<QuestionGenerationRequest>().Count));

        QuestionGenerator
            .GenerateSubtopicsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([]);

        Translator
            .TranslateAsync(Arg.Any<string>(), Arg.Any<Language>(), Arg.Any<Language>(), Arg.Any<CancellationToken>())
            .Returns(call => $"{call.ArgAt<string>(0)} ({call.ArgAt<Language>(2)})");
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

        builder.UseSetting(
            $"{SignInRateLimitOptions.SectionName}:{nameof(SignInRateLimitOptions.PermitLimit)}",
            SignInPermitLimit.ToString());

        builder.UseSetting(
            $"{SignInRateLimitOptions.SectionName}:{nameof(SignInRateLimitOptions.AddressPermitLimit)}",
            SignInAddressPermitLimit.ToString());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IQuestionGenerator>();
            services.AddScoped(_ => QuestionGenerator);
            services.RemoveAll<ITextTranslator>();
            services.AddScoped(_ => Translator);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ToJePrivelaDbContext>();
        context.Database.Migrate();

        // Like the first configured admin, the test admin takes over the reserved account and its seeded players.
        var now = DateTime.UtcNow;
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        Admin = context.Accounts.Single(account => account.Id == Account.ReservedId);
        Admin.TakeOverReserved("Test admin", hasher.Hash(AdminPassword), now);
        Member = Account.Register("Test member", hasher.Hash(MemberPassword), now);
        context.Accounts.Add(Member);
        context.SaveChanges();

        // No category is seeded any more; tests use these, with the ids they had as seed data.
        context.Database.ExecuteSqlRaw("""
            INSERT INTO "QuestionCategories" ("Id", "NameSk", "NameSkKey", "NameEn", "NameEnKey") VALUES
                (1, 'Autá', 'autá', 'Cars', 'cars'),
                (2, 'Šport', 'šport', 'Sport', 'sport'),
                (3, 'História', 'história', 'History', 'history');
            """);

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
            // Pooled connections keep the file open.
            SqliteConnection.ClearAllPools();
            File.Delete(_databasePath);
        }
    }

    private IReadOnlyList<GeneratedQuestion> Fresh(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => Interlocked.Increment(ref _generatedCount))
            .Select(number => new GeneratedQuestion($"Generated test question number {number}?", (number + 1000).ToString()))
            .ToList();
}

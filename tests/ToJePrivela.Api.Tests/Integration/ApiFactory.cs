using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Api.Tests.Integration;

/// <summary>Hosts the real API on a throwaway SQLite file with the AI provider stubbed out.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"tojeprivela-tests-{Guid.NewGuid():N}.db");
    private int _generatedCount;

    public ApiFactory()
    {
        ReplyWithFreshQuestions();
    }

    public IQuestionGenerator QuestionGenerator { get; } = Substitute.For<IQuestionGenerator>();

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

        builder.UseSetting(
            $"{AiRateLimitOptions.SectionName}:{nameof(AiRateLimitOptions.PermitLimit)}",
            AiGenerationPermitLimit.ToString());

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IQuestionGenerator>();
            services.AddScoped(_ => QuestionGenerator);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ToJePrivelaDbContext>().Database.Migrate();

        return host;
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
            .Select(number => new GeneratedQuestion($"Generated test question number {number}?", number.ToString()))
            .ToList();
}

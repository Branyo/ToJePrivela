using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Api.Tests.Integration;

/// <summary>Hosts the real API on a throwaway SQLite file with the AI provider stubbed out.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"tojeprivela-tests-{Guid.NewGuid():N}.db");

    public IQuestionGenerator QuestionGenerator { get; } = Substitute.For<IQuestionGenerator>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            $"ConnectionStrings:{SqliteConnectionString.Name}",
            $"Data Source={_databasePath}");

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
}

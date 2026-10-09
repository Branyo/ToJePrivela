using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Infrastructure.Persistence;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure;

public static class DependencyInjection
{
    /// <summary>How long the /api/health database query may wait for a locked SQLite file before reporting Unhealthy.</summary>
    public static readonly TimeSpan HealthCheckQueryTimeout = TimeSpan.FromSeconds(2);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ToJePrivelaDbContext>(options =>
            options.UseSqlite(SqliteConnectionString.Resolve(configuration)));

        // Readiness, reported by /api/health: the database must answer a real query, which also needs the migrated
        // schema (a bare connection test would pass for an empty or unmigrated SQLite file). A locked file must turn
        // into a quick Unhealthy, not a probe left waiting for SQLite's default 30-second busy timeout.
        services.AddHealthChecks().AddDbContextCheck<ToJePrivelaDbContext>(
            name: "database",
            customTestQuery: (db, cancellationToken) =>
            {
                db.Database.SetCommandTimeout(HealthCheckQueryTimeout);
                return db.Players.AnyAsync(cancellationToken);
            });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IQuestionCategoryRepository, QuestionCategoryRepository>();

        return services;
    }
}

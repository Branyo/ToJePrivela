using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Infrastructure.Persistence;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ToJePrivelaDbContext>(options =>
            options.UseSqlite(SqliteConnectionString.Resolve(configuration)));

        // Readiness, reported by /api/health: the database must answer a real query, which also needs the migrated
        // schema (a bare connection test would pass for an empty or unmigrated SQLite file).
        services.AddHealthChecks().AddDbContextCheck<ToJePrivelaDbContext>(
            customTestQuery: (db, cancellationToken) => db.Players.AnyAsync(cancellationToken));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IQuestionCategoryRepository, QuestionCategoryRepository>();

        return services;
    }
}

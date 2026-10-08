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

        // Readiness: the database must answer, which /api/health reports.
        services.AddHealthChecks().AddDbContextCheck<ToJePrivelaDbContext>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IPlayerRepository, PlayerRepository>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IQuestionCategoryRepository, QuestionCategoryRepository>();

        return services;
    }
}

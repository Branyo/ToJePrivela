using Microsoft.EntityFrameworkCore;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Api.Common;

public static class DatabaseInitializer
{
    /// <summary>Applies pending migrations so a fresh clone runs without a manual EF step.</summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<ToJePrivelaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ToJePrivelaDbContext>>();

        await context.Database.MigrateAsync();
        logger.LogInformation("Database is up to date ({ConnectionString}).", context.Database.GetConnectionString());
    }
}

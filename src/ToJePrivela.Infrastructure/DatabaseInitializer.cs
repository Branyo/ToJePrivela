using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure;

public static class DatabaseInitializer
{
    /// <summary>
    /// Applies pending migrations so a fresh clone runs without a manual EF step, and puts the database in WAL mode.
    /// Lives here so the host never has to know which DbContext or database provider sits behind the repositories.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<ToJePrivelaDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ToJePrivelaDbContext>>();

        await context.Database.MigrateAsync(cancellationToken);

        // EF only turns WAL on for a file it creates; a database made or restored elsewhere keeps its rollback journal,
        // where a reader blocks writers. WAL lets requests save while others read (GameRepository's split reads rely on
        // it), and the file keeps the mode once set.
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = 'wal';", cancellationToken);

        // Only the file is logged: a full connection string may carry credentials.
        var dataSource = new SqliteConnectionStringBuilder(context.Database.GetConnectionString()).DataSource;
        logger.LogInformation("Database is up to date ({DataSource}).", dataSource);
    }
}

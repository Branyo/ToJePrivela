using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public sealed class DatabaseInitializerTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"tojeprivela-init-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task InitializeDatabaseAsync_CreatesAndSeedsAFreshDatabase()
    {
        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddDbContext<ToJePrivelaDbContext>(options => options.UseSqlite($"Data Source={_databasePath};Pooling=False"))
            .BuildServiceProvider();

        await provider.InitializeDatabaseAsync();

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ToJePrivelaDbContext>();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.NotEmpty(await context.Players.ToListAsync());
    }

    [Fact]
    public async Task InitializeDatabaseAsync_PutsADatabaseMadeElsewhereInWalMode()
    {
        // As a database created or restored outside EF would be: in SQLite's default rollback journal mode.
        await using (var created = new SqliteConnection($"Data Source={_databasePath};Pooling=False"))
        {
            await created.OpenAsync();
            await using var command = created.CreateCommand();
            command.CommandText = "PRAGMA journal_mode = 'delete'; CREATE TABLE \"Marker\" (\"Id\" INTEGER);";
            await command.ExecuteNonQueryAsync();
        }

        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddDbContext<ToJePrivelaDbContext>(options => options.UseSqlite($"Data Source={_databasePath};Pooling=False"))
            .BuildServiceProvider();

        await provider.InitializeDatabaseAsync();

        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ToJePrivelaDbContext>();
        Assert.Equal("wal", context.Database.SqlQueryRaw<string>("PRAGMA journal_mode").AsEnumerable().Single());
    }

    public void Dispose()
    {
        foreach (var file in new[] { _databasePath, _databasePath + "-wal", _databasePath + "-shm" })
        {
            File.Delete(file);
        }
    }
}

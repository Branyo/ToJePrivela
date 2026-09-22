using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>A migrated in-memory database; it lives as long as the connection stays open.</summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.Migrate();
    }

    public ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}

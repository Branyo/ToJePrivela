using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

/// <summary>
/// A migrated in-memory database; it lives as long as the connection stays open. No category is seeded, so unless told
/// otherwise it adds the test categories Cars (1), Sport (2) and History (3).
/// </summary>
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public SqliteTestDatabase(bool withTestCategories = true)
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.Migrate();

        if (withTestCategories)
        {
            context.Database.ExecuteSqlRaw("""
                INSERT INTO "QuestionCategories" ("Id", "NameSk", "NameSkKey", "NameEn", "NameEnKey") VALUES
                    (1, 'Autá', 'autá', 'Cars', 'cars'),
                    (2, 'Šport', 'šport', 'Sport', 'sport'),
                    (3, 'História', 'história', 'History', 'history');
                """);
        }
    }

    public ToJePrivelaDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ToJePrivelaDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}

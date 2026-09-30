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

    public void Dispose()
    {
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}

using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class AccountRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 6, 18, 0, 0, DateTimeKind.Utc);

    private readonly SqliteTestDatabase _database = new();

    [Fact]
    public async Task Migrations_InsertTheReservedAccount()
    {
        await using var context = _database.CreateContext();

        var reserved = await new AccountRepository(context).GetReservedAsync();

        Assert.NotNull(reserved);
        Assert.Equal(Account.ReservedId, reserved!.Id);
        Assert.True(reserved.IsReserved);
        Assert.False(reserved.IsAdmin);
    }

    [Fact]
    public async Task GetReservedAsync_FindsNothingOnceAnAdminTookTheReservedAccount()
    {
        await using (var context = _database.CreateContext())
        {
            var reserved = await new AccountRepository(context).GetReservedAsync();
            reserved!.TakeOverReserved("Brano", "hash", Now);
            await context.SaveChangesAsync();
        }

        await using var fresh = _database.CreateContext();
        var repository = new AccountRepository(fresh);

        Assert.Null(await repository.GetReservedAsync());
        Assert.Equal(Account.ReservedId, (await repository.GetByNameAsync("brano"))!.Id);
    }

    [Theory]
    [InlineData("Brano")]
    [InlineData("BRANO")]
    [InlineData("  brano ")]
    public async Task GetByNameAsync_IgnoresCaseAndSurroundingSpaces(string name)
    {
        await StoreAsync(Account.Register("Brano", "hash", Now));

        await using var context = _database.CreateContext();

        Assert.Equal("Brano", (await new AccountRepository(context).GetByNameAsync(name))!.Name);
    }

    [Fact]
    public async Task GetByNameAsync_FoldsAccentedLetters()
    {
        await StoreAsync(Account.Register("Štefan", "hash", Now));

        await using var context = _database.CreateContext();
        var repository = new AccountRepository(context);

        Assert.NotNull(await repository.GetByNameAsync("štefan"));
        Assert.Null(await repository.GetByNameAsync("Stefan"));
    }

    [Fact]
    public async Task GetByNameAsync_NeverFindsTheReservedAccount()
    {
        await using var context = _database.CreateContext();

        Assert.Null(await new AccountRepository(context).GetByNameAsync("   "));
    }

    [Fact]
    public async Task GetAdminsAsync_ReturnsOnlyAdmins()
    {
        await StoreAsync(Account.CreateAdmin("Brano", "hash", Now));
        await StoreAsync(Account.Register("Duri", "hash", Now));

        await using var context = _database.CreateContext();

        Assert.Equal(["Brano"], (await new AccountRepository(context).GetAdminsAsync()).Select(a => a.Name));
    }

    [Fact]
    public async Task SaveChanges_RejectsASecondLoginWithTheSameName()
    {
        await StoreAsync(Account.Register("Štefan", "hash", Now));

        await using var context = _database.CreateContext();
        await new AccountRepository(context).AddAsync(Account.Register("ŠTEFAN", "hash", Now));

        await Assert.ThrowsAsync<UniqueConstraintException>(() => new UnitOfWork(context).SaveChangesAsync());
    }

    [Fact]
    public async Task StoredTimes_ComeBackAsUtc()
    {
        var account = Account.Register("Brano", "hash", Now);
        account.RecordSignIn(Now.AddMinutes(5));
        await StoreAsync(account);

        await using var context = _database.CreateContext();
        var stored = (await new AccountRepository(context).GetByNameAsync("Brano"))!;

        Assert.Equal(DateTimeKind.Utc, stored.CreatedAt.Kind);
        Assert.Equal(Now.AddMinutes(5), stored.LastSignedInAt);
    }

    private async Task StoreAsync(Account account)
    {
        await using var context = _database.CreateContext();
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _database.Dispose();
}

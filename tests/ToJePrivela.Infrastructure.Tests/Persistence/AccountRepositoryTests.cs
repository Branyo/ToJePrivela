using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;
using ToJePrivela.Infrastructure.Persistence.Repositories;

namespace ToJePrivela.Infrastructure.Tests.Persistence;

public class AccountRepositoryTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 10, 3, 18, 0, 0, DateTimeKind.Utc);

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
            reserved!.ProvisionFor(IdentityProvider.Google, "admin@example.com");
            await context.SaveChangesAsync();
        }

        await using var fresh = _database.CreateContext();
        var repository = new AccountRepository(fresh);

        Assert.Null(await repository.GetReservedAsync());
        Assert.Equal(
            Account.ReservedId,
            (await repository.GetProvisionedAsync(IdentityProvider.Google, "admin@example.com"))!.Id);
    }

    [Fact]
    public async Task GetBoundAsync_MatchesProviderAndExternalId()
    {
        await StoreAsync(Account.Register(IdentityProvider.Google, "same-id", null, "Google user", Now));
        await StoreAsync(Account.Register(IdentityProvider.Facebook, "same-id", null, "Facebook user", Now));

        await using var context = _database.CreateContext();
        var repository = new AccountRepository(context);

        Assert.Equal("Google user", (await repository.GetBoundAsync(IdentityProvider.Google, "same-id"))!.DisplayName);
        Assert.Equal("Facebook user", (await repository.GetBoundAsync(IdentityProvider.Facebook, "same-id"))!.DisplayName);
        Assert.Null(await repository.GetBoundAsync(IdentityProvider.Google, "other"));
    }

    [Fact]
    public async Task GetProvisionedAsync_IgnoresBoundAccountsAndOtherProviders()
    {
        await StoreAsync(Account.Register(IdentityProvider.Google, "sub", "bound@example.com", "Bound", Now));
        await StoreAsync(Account.ProvisionAdmin(IdentityProvider.Facebook, "admin@example.com"));

        await using var context = _database.CreateContext();
        var repository = new AccountRepository(context);

        Assert.Null(await repository.GetProvisionedAsync(IdentityProvider.Google, "bound@example.com"));
        Assert.Null(await repository.GetProvisionedAsync(IdentityProvider.Google, "admin@example.com"));
        Assert.NotNull(await repository.GetProvisionedAsync(IdentityProvider.Facebook, "admin@example.com"));
    }

    [Fact]
    public async Task GetByEmailAsync_FindsBoundAndProvisionedAccounts()
    {
        await StoreAsync(Account.Register(IdentityProvider.Google, "sub", "bound@example.com", "Bound", Now));
        await StoreAsync(Account.ProvisionAdmin(IdentityProvider.Google, "admin@example.com"));

        await using var context = _database.CreateContext();
        var repository = new AccountRepository(context);

        Assert.NotNull(await repository.GetByEmailAsync(IdentityProvider.Google, "bound@example.com"));
        Assert.NotNull(await repository.GetByEmailAsync(IdentityProvider.Google, "admin@example.com"));
    }

    [Fact]
    public async Task SaveChanges_RejectsASecondAccountForTheSameIdentity()
    {
        await StoreAsync(Account.Register(IdentityProvider.Google, "sub", null, "First", Now));

        await using var context = _database.CreateContext();
        await new AccountRepository(context).AddAsync(Account.Register(IdentityProvider.Google, "sub", null, "Second", Now));

        await Assert.ThrowsAsync<UniqueConstraintException>(() => new UnitOfWork(context).SaveChangesAsync());
    }

    [Fact]
    public async Task SaveChanges_AllowsManyProvisionedAccounts()
    {
        await StoreAsync(Account.ProvisionAdmin(IdentityProvider.Google, "one@example.com"));
        await StoreAsync(Account.ProvisionAdmin(IdentityProvider.Google, "two@example.com"));

        await using var context = _database.CreateContext();

        Assert.Equal(3, await context.Accounts.CountAsync());
    }

    private async Task StoreAsync(Account account)
    {
        await using var context = _database.CreateContext();
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _database.Dispose();
}

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Accounts;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Accounts;

public class AdminAccountProvisionerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 18, 0, 0, TimeSpan.Zero);

    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public AdminAccountProvisionerTests()
    {
        _accounts.GetAdminsAsync(Arg.Any<CancellationToken>()).Returns([]);
    }

    [Fact]
    public async Task ProvisionAsync_HandsTheReservedAccountToTheFirstAdminWithoutALogin()
    {
        var reserved = TestEntities.ReservedAccount();
        _accounts.GetReservedAsync(Arg.Any<CancellationToken>()).Returns(reserved);

        await Provisioner(Admin("Brano", "brano-password")).ProvisionAsync();

        Assert.False(reserved.IsReserved);
        Assert.True(reserved.IsAdmin);
        Assert.Equal("Brano", reserved.Name);
        Assert.Equal(FakePasswordHasher.HashOf("brano-password"), reserved.PasswordHash);
        await _accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_CreatesTheOtherAdminsAsNewLogins()
    {
        _accounts.GetReservedAsync(Arg.Any<CancellationToken>()).Returns(TestEntities.ReservedAccount());

        await Provisioner(Admin("Brano", "brano-password"), Admin("Duri", "duri-password")).ProvisionAsync();

        await _accounts.Received(1).AddAsync(
            Arg.Is<Account>(a => a.Name == "Duri" && a.IsAdmin && a.PasswordHash == FakePasswordHasher.HashOf("duri-password")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_MakesAnExistingLoginAdminAndSetsTheConfiguredPassword()
    {
        // Someone created "Brano" before it was configured: they must not become admin with their own password.
        var existing = Existing(TestEntities.Account(4, "Brano", passwordHash: FakePasswordHasher.HashOf("squatter-password")));

        await Provisioner(Admin("brano", "brano-password")).ProvisionAsync();

        Assert.True(existing.IsAdmin);
        Assert.Equal(FakePasswordHasher.HashOf("brano-password"), existing.PasswordHash);
        await _accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_KeepsTheHashOfAnAdminWhosePasswordIsUnchanged()
    {
        var hash = FakePasswordHasher.HashOf("brano-password");
        var existing = Existing(TestEntities.Account(4, "Brano", isAdmin: true, passwordHash: hash));

        await Provisioner(Admin("Brano", "brano-password")).ProvisionAsync();

        Assert.Same(hash, existing.PasswordHash);
    }

    [Fact]
    public async Task ProvisionAsync_RevokesAdminsThatAreNoLongerConfigured()
    {
        var kept = Existing(TestEntities.Account(4, "Brano", isAdmin: true, passwordHash: FakePasswordHasher.HashOf("brano-password")));
        var former = TestEntities.Account(5, "Duri", isAdmin: true);
        _accounts.GetAdminsAsync(Arg.Any<CancellationToken>()).Returns([kept, former]);

        await Provisioner(Admin("Brano", "brano-password")).ProvisionAsync();

        Assert.True(kept.IsAdmin);
        Assert.False(former.IsAdmin);
    }

    [Fact]
    public async Task ProvisionAsync_RevokesEveryAdminWhenNoneIsConfigured()
    {
        var former = TestEntities.Account(5, "Duri", isAdmin: true);
        _accounts.GetAdminsAsync(Arg.Any<CancellationToken>()).Returns([former]);

        await Provisioner().ProvisionAsync();

        Assert.False(former.IsAdmin);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_TreatsTheSameNameConfiguredTwiceAsOneAdmin()
    {
        await Provisioner(Admin("Brano", "brano-password"), Admin(" BRANO ", "other-password")).ProvisionAsync();

        await _accounts.Received(1).AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    private Account Existing(Account account)
    {
        _accounts.GetByNameAsync(Arg.Is<string>(n => NameKeys.Of(n) == account.NameKey), Arg.Any<CancellationToken>())
            .Returns(account);
        return account;
    }

    private AdminAccountProvisioner Provisioner(params AdminLogin[] admins) =>
        new(
            _accounts,
            _unitOfWork,
            new FakePasswordHasher(),
            new FixedTimeProvider(Now),
            Options.Create(new AdminAccountsOptions { Admins = [.. admins] }),
            NullLogger<AdminAccountProvisioner>.Instance);

    private static AdminLogin Admin(string name, string password) => new() { Name = name, Password = password };
}

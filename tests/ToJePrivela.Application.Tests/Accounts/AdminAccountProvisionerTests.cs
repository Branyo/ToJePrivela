using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Accounts;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Accounts;

public class AdminAccountProvisionerTests
{
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    [Fact]
    public async Task ProvisionAsync_HandsTheReservedAccountToTheFirstAdmin()
    {
        var reserved = TestEntities.ReservedAccount();
        _accounts.GetReservedAsync(Arg.Any<CancellationToken>()).Returns(reserved);

        await Provisioner(Admin(IdentityProvider.Google, "First@Example.com")).ProvisionAsync();

        Assert.True(reserved.IsAdmin);
        Assert.Equal("first@example.com", reserved.Email);
        Assert.False(reserved.IsReserved);
        await _accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_ProvisionsTheOtherAdminsAsNewAccounts()
    {
        _accounts.GetReservedAsync(Arg.Any<CancellationToken>()).Returns(TestEntities.ReservedAccount());

        await Provisioner(
            Admin(IdentityProvider.Google, "first@example.com"),
            Admin(IdentityProvider.Facebook, "second@example.com")).ProvisionAsync();

        await _accounts.Received(1).AddAsync(
            Arg.Is<Account>(a => a.Provider == IdentityProvider.Facebook && a.Email == "second@example.com" && a.IsAdmin),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_MakesAnExistingAccountAdmin()
    {
        var existing = TestEntities.Account(4, "brano");
        _accounts.GetByEmailAsync(IdentityProvider.Google, "brano@example.com", Arg.Any<CancellationToken>())
            .Returns(existing);
        var reserved = TestEntities.ReservedAccount();
        _accounts.GetReservedAsync(Arg.Any<CancellationToken>()).Returns(reserved);

        await Provisioner(Admin(IdentityProvider.Google, "brano@example.com")).ProvisionAsync();

        Assert.True(existing.IsAdmin);
        Assert.True(reserved.IsReserved);
        await _accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_ProvisionsTheFirstAdminWhenTheReservedAccountIsTaken()
    {
        _accounts.GetReservedAsync(Arg.Any<CancellationToken>()).Returns((Account?)null);

        await Provisioner(Admin(IdentityProvider.Google, "first@example.com")).ProvisionAsync();

        await _accounts.Received(1).AddAsync(
            Arg.Is<Account>(a => a.Email == "first@example.com" && a.IsAdmin),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProvisionAsync_DoesNothingWithoutConfiguredAdmins()
    {
        await Provisioner().ProvisionAsync();

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    private AdminAccountProvisioner Provisioner(params AdminLogin[] admins) =>
        new(
            _accounts,
            _unitOfWork,
            Options.Create(new AdminAccountsOptions { Admins = [.. admins] }),
            NullLogger<AdminAccountProvisioner>.Instance);

    private static AdminLogin Admin(IdentityProvider provider, string email) => new() { Provider = provider, Email = email };
}

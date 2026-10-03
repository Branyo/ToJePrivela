using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class AccountTests
{
    private static readonly DateTime Now = new(2026, 10, 3, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Register_CreatesABoundAccountThatIsNoAdmin()
    {
        var account = Account.Register(IdentityProvider.Google, "sub-1", " Brano@Example.com ", "Brano", Now);

        Assert.True(account.IsBound);
        Assert.False(account.IsReserved);
        Assert.False(account.IsAdmin);
        Assert.Equal("sub-1", account.ExternalId);
        Assert.Equal("brano@example.com", account.Email);
        Assert.Equal("Brano", account.DisplayName);
        Assert.Equal(Now, account.LastSignedInAt);
    }

    [Fact]
    public void Register_FallsBackToTheEmailWhenTheNameIsMissing()
    {
        var account = Account.Register(IdentityProvider.Facebook, "42", "duri@example.com", " ", Now);

        Assert.Equal("duri@example.com", account.DisplayName);
    }

    [Fact]
    public void Register_CutsAnOverlongNameToTheColumnSize()
    {
        var account = Account.Register(IdentityProvider.Google, "sub", null, new string('x', 300), Now);

        Assert.Equal(Account.DisplayNameMaxLength, account.DisplayName.Length);
    }

    [Fact]
    public void Register_RejectsAnEmailWithoutAnAt()
    {
        Assert.Throws<DomainException>(() => Account.Register(IdentityProvider.Google, "sub", "nobody", "N", Now));
    }

    [Fact]
    public void Register_RejectsAnUnknownProvider()
    {
        Assert.Throws<DomainException>(() => Account.Register((IdentityProvider)9, "sub", null, "N", Now));
    }

    [Fact]
    public void ProvisionAdmin_CreatesAnUnboundAdminKnownByEmail()
    {
        var account = Account.ProvisionAdmin(IdentityProvider.Google, "Admin@Example.com");

        Assert.False(account.IsBound);
        Assert.False(account.IsReserved);
        Assert.True(account.IsAdmin);
        Assert.Equal("admin@example.com", account.Email);
    }

    [Fact]
    public void SignIn_BindsAProvisionedAccountAndKeepsItAdmin()
    {
        var account = Account.ProvisionAdmin(IdentityProvider.Google, "admin@example.com");

        account.SignIn("sub-9", "admin@example.com", "Admin", Now);

        Assert.True(account.IsBound);
        Assert.True(account.IsAdmin);
        Assert.Equal("sub-9", account.ExternalId);
        Assert.Equal("Admin", account.DisplayName);
        Assert.Equal(Now, account.LastSignedInAt);
    }

    [Fact]
    public void SignIn_RefreshesNameAndEmailButKeepsTheEmailWhenNoneIsGiven()
    {
        var account = Account.Register(IdentityProvider.Google, "sub", "old@example.com", "Old", Now);

        account.SignIn("sub", null, "New", Now.AddDays(1));

        Assert.Equal("old@example.com", account.Email);
        Assert.Equal("New", account.DisplayName);
        Assert.Equal(Now.AddDays(1), account.LastSignedInAt);
    }

    [Fact]
    public void SignIn_RejectsAnotherIdentity()
    {
        var account = Account.Register(IdentityProvider.Google, "sub", null, "N", Now);

        Assert.Throws<DomainException>(() => account.SignIn("other", null, "N", Now));
    }

    [Fact]
    public void GrantAdmin_MakesTheAccountAnAdmin()
    {
        var account = Account.Register(IdentityProvider.Google, "sub", null, "N", Now);

        account.GrantAdmin();

        Assert.True(account.IsAdmin);
    }

    [Fact]
    public void ProvisionFor_RejectsAnAccountThatIsNotReserved()
    {
        var account = Account.ProvisionAdmin(IdentityProvider.Google, "admin@example.com");

        Assert.Throws<DomainException>(() => account.ProvisionFor(IdentityProvider.Google, "other@example.com"));
    }
}

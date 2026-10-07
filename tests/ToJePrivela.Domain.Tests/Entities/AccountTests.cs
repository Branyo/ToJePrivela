using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class AccountTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);
    private const string StampPattern = "^[0-9a-f]{32}$";

    [Fact]
    public void Register_CreatesALoginThatIsNoAdmin()
    {
        var account = Account.Register("  Brano  ", "hash", Now);

        Assert.Equal("Brano", account.Name);
        Assert.Equal("brano", account.NameKey);
        Assert.Equal("hash", account.PasswordHash);
        Assert.False(account.IsAdmin);
        Assert.False(account.IsReserved);
        Assert.Equal(Now, account.CreatedAt);
        Assert.Null(account.LastSignedInAt);
    }

    [Fact]
    public void CreateAdmin_CreatesAnAdmin()
    {
        Assert.True(Account.CreateAdmin("Brano", "hash", Now).IsAdmin);
    }

    [Fact]
    public void NameKey_FoldsAccentedLettersToo()
    {
        Assert.Equal(Account.Register("ŠTEFAN", "hash", Now).NameKey, Account.Register("štefan", "hash", Now).NameKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    [InlineData("1234567890123456789012345678901")]
    public void Register_RejectsAnInvalidName(string name)
    {
        Assert.Throws<DomainException>(() => Account.Register(name, "hash", Now));
    }

    [Fact]
    public void Register_RequiresAPasswordHash()
    {
        Assert.Throws<DomainException>(() => Account.Register("Brano", " ", Now));
    }

    [Fact]
    public void RecordSignIn_StoresTheTimeInUtc()
    {
        var account = Account.Register("Brano", "hash", Now);
        var local = new DateTime(2026, 10, 6, 14, 0, 0, DateTimeKind.Unspecified);

        account.RecordSignIn(local);

        Assert.Equal(DateTimeKind.Utc, account.LastSignedInAt!.Value.Kind);
    }

    [Fact]
    public void GrantAndRevokeAdmin_ToggleTheFlag()
    {
        var account = Account.Register("Brano", "hash", Now);

        account.GrantAdmin();
        Assert.True(account.IsAdmin);

        account.RevokeAdmin();
        Assert.False(account.IsAdmin);
    }

    [Fact]
    public void ChangePasswordHash_ReplacesTheHash()
    {
        var account = Account.Register("Brano", "old", Now);

        account.ChangePasswordHash("new");

        Assert.Equal("new", account.PasswordHash);
    }

    [Fact]
    public void Register_GivesEveryLoginItsOwnSecurityStamp()
    {
        var first = Account.Register("Brano", "hash", Now);
        var second = Account.Register("Duri", "hash", Now);

        Assert.Matches(StampPattern, first.SecurityStamp);
        Assert.NotEqual(first.SecurityStamp, second.SecurityStamp);
    }

    [Fact]
    public void ChangePasswordHash_EndsEarlierSignIns()
    {
        var account = Account.Register("Brano", "old", Now);
        var before = account.SecurityStamp;

        account.ChangePasswordHash("new");

        Assert.NotEqual(before, account.SecurityStamp);
    }

    [Fact]
    public void UpgradePasswordHash_KeepsEarlierSignIns()
    {
        var account = Account.Register("Brano", "weak", Now);
        var before = account.SecurityStamp;

        account.UpgradePasswordHash("strong");

        Assert.Equal("strong", account.PasswordHash);
        Assert.Equal(before, account.SecurityStamp);
    }

    [Fact]
    public void GrantAdmin_EndsTheSignInsOfALoginThatWasNoAdmin()
    {
        var account = Account.Register("Brano", "hash", Now);
        var before = account.SecurityStamp;

        account.GrantAdmin();

        Assert.NotEqual(before, account.SecurityStamp);
    }

    [Fact]
    public void GrantAdmin_KeepsTheSignInsOfAnAdmin()
    {
        // The configured admins are granted again on every startup; that must not sign them out.
        var account = Account.CreateAdmin("Brano", "hash", Now);
        var before = account.SecurityStamp;

        account.GrantAdmin();

        Assert.Equal(before, account.SecurityStamp);
    }

    [Fact]
    public void RevokeAdmin_KeepsTheSignIns()
    {
        // Rights are read on every request, so the login simply stops being admin.
        var account = Account.CreateAdmin("Brano", "hash", Now);
        var before = account.SecurityStamp;

        account.RevokeAdmin();

        Assert.Equal(before, account.SecurityStamp);
    }

    [Fact]
    public void TakeOverReserved_GivesTheReservedAccountToAnAdmin()
    {
        var reserved = Reserved();

        reserved.TakeOverReserved("Brano", "hash", Now);
        Assert.Matches(StampPattern, reserved.SecurityStamp);

        Assert.False(reserved.IsReserved);
        Assert.True(reserved.IsAdmin);
        Assert.Equal("Brano", reserved.Name);
        Assert.Equal("brano", reserved.NameKey);
        Assert.Equal(Now, reserved.CreatedAt);
    }

    [Fact]
    public void TakeOverReserved_RefusesAnAccountThatIsNotReserved()
    {
        var account = Account.Register("Brano", "hash", Now);

        Assert.Throws<DomainException>(() => account.TakeOverReserved("Duri", "hash", Now));
    }

    [Fact]
    public void TheReservedAccount_CannotSignInOrBeMadeAdminBeforeItIsTakenOver()
    {
        var reserved = Reserved();

        Assert.True(reserved.IsReserved);
        Assert.Throws<DomainException>(() => reserved.RecordSignIn(Now));
        Assert.Throws<DomainException>(() => reserved.GrantAdmin());
        Assert.Throws<DomainException>(() => reserved.ChangePasswordHash("hash"));
        Assert.Throws<DomainException>(() => reserved.UpgradePasswordHash("hash"));
    }

    /// <summary>The row the migration inserts; the domain itself never creates one.</summary>
    private static Account Reserved() => (Account)Activator.CreateInstance(typeof(Account), nonPublic: true)!;
}

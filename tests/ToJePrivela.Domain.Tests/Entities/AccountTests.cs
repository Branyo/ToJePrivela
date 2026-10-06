using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class AccountTests
{
    private static readonly DateTime Now = new(2026, 10, 6, 12, 0, 0, DateTimeKind.Utc);

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
    public void TakeOverReserved_GivesTheReservedAccountToAnAdmin()
    {
        var reserved = Reserved();

        reserved.TakeOverReserved("Brano", "hash", Now);

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
    }

    /// <summary>The row the migration inserts; the domain itself never creates one.</summary>
    private static Account Reserved() => (Account)Activator.CreateInstance(typeof(Account), nonPublic: true)!;
}

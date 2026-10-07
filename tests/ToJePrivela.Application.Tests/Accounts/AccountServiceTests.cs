using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Accounts;
using ToJePrivela.Application.Accounts.Dtos;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Accounts;

public class AccountServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 18, 0, 0, TimeSpan.Zero);
    private static readonly AccessToken Token = new("issued-token", Now.UtcDateTime.AddHours(12));

    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAccessTokenIssuer _tokenIssuer = Substitute.For<IAccessTokenIssuer>();
    private readonly ICurrentAccount _currentAccount = Substitute.For<ICurrentAccount>();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _tokenIssuer.Issue(Arg.Any<Account>()).Returns(Token);

        _sut = new AccountService(
            _accounts,
            _unitOfWork,
            new FakePasswordHasher(),
            _tokenIssuer,
            _currentAccount,
            new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task SignInAsync_ReturnsATokenForTheRightPassword()
    {
        var account = Stored(5, "Brano", "secret-password");

        var result = await _sut.SignInAsync(new SignInRequest { Name = "  brano ", Password = "secret-password" });

        Assert.True(result.IsSuccess);
        Assert.Equal("issued-token", result.Value.AccessToken);
        Assert.Equal(Token.ExpiresAt, result.Value.ExpiresAt);
        Assert.Equal(new AccountDto(5, "Brano", false), result.Value.Account);
        Assert.Equal(Now.UtcDateTime, account.LastSignedInAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignInAsync_RefusesAWrongPassword()
    {
        var account = Stored(5, "Brano", "secret-password");

        var result = await _sut.SignInAsync(new SignInRequest { Name = "Brano", Password = "wrong-password" });

        Assert.Equal(AccountErrors.WrongPassword, result.Error);
        Assert.Null(account.LastSignedInAt);
        _tokenIssuer.DidNotReceive().Issue(Arg.Any<Account>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignInAsync_SaysWhenNoLoginHasTheName()
    {
        var result = await _sut.SignInAsync(new SignInRequest { Name = " Nobody ", Password = "secret-password" });

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Auth.UnknownLogin", result.Error.Code);
        Assert.Contains("'Nobody'", result.Error.Message);
    }

    [Fact]
    public async Task SignInAsync_NeverSignsInToTheReservedAccount()
    {
        _accounts.GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(TestEntities.ReservedAccount());

        var result = await _sut.SignInAsync(new SignInRequest { Name = "anything", Password = "secret-password" });

        Assert.Equal("Auth.UnknownLogin", result.Error.Code);
    }

    [Fact]
    public async Task SignInAsync_ReplacesAnOutdatedHash()
    {
        var account = TestEntities.Account(5, "Brano", passwordHash: "old:secret-password");
        _accounts.GetByNameAsync("Brano", Arg.Any<CancellationToken>()).Returns(account);

        var result = await _sut.SignInAsync(new SignInRequest { Name = "Brano", Password = "secret-password" });

        Assert.True(result.IsSuccess);
        Assert.Equal(FakePasswordHasher.HashOf("secret-password"), account.PasswordHash);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task SignInAsync_WithoutAPassword_SaysWhetherTheLoginExists(string? password)
    {
        Stored(5, "Brano", "secret-password");

        var known = await _sut.SignInAsync(new SignInRequest { Name = "Brano", Password = password });
        var unknown = await _sut.SignInAsync(new SignInRequest { Name = "Nobody", Password = password });

        Assert.Equal(AccountErrors.WrongPassword, known.Error);
        Assert.Equal("Auth.UnknownLogin", unknown.Error.Code);
    }

    [Fact]
    public async Task SignInAsync_RejectsAMissingName()
    {
        var result = await _sut.SignInAsync(new SignInRequest { Name = " ", Password = "secret-password" });

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _accounts.DidNotReceive().GetByNameAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_StoresAHashedPasswordAndSignsTheNewLoginIn()
    {
        Account? added = null;
        await _accounts.AddAsync(Arg.Do<Account>(a => added = a), Arg.Any<CancellationToken>());

        var result = await _sut.CreateAsync(new CreateAccountRequest { Name = "  Brano ", Password = "secret-password" });

        Assert.True(result.IsSuccess);
        Assert.Equal("issued-token", result.Value.AccessToken);
        Assert.Equal("Brano", result.Value.Account.Name);
        Assert.False(result.Value.Account.IsAdmin);
        Assert.NotNull(added);
        Assert.Equal(FakePasswordHasher.HashOf("secret-password"), added.PasswordHash);
        Assert.Equal(Now.UtcDateTime, added.CreatedAt);
        Assert.Equal(Now.UtcDateTime, added.LastSignedInAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RefusesATakenName()
    {
        Stored(5, "Brano", "secret-password");

        var result = await _sut.CreateAsync(new CreateAccountRequest { Name = "Brano", Password = "other-password" });

        Assert.Equal(AccountErrors.NameTaken("Brano"), result.Error);
        await _accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReportsANameTakenByARacingRequest()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new UniqueConstraintException("duplicate", new Exception()));

        var result = await _sut.CreateAsync(new CreateAccountRequest { Name = "Brano", Password = "secret-password" });

        Assert.Equal("Auth.NameTaken", result.Error.Code);
        _tokenIssuer.DidNotReceive().Issue(Arg.Any<Account>());
    }

    [Theory]
    [InlineData("ab", "secret-password")]
    [InlineData("Brano", "short")]
    [InlineData("Brano", "")]
    public async Task CreateAsync_RejectsAnInvalidNameOrPassword(string name, string password)
    {
        var result = await _sut.CreateAsync(new CreateAccountRequest { Name = name, Password = password });

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RejectsAPasswordLongerThanTheLimit()
    {
        var password = new string('x', PasswordRules.MaxLength + 1);

        var result = await _sut.CreateAsync(new CreateAccountRequest { Name = "Brano", Password = password });

        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsTheSignedInAccount()
    {
        _currentAccount.Id.Returns(7);
        _accounts.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(TestEntities.Account(7, "Brano", isAdmin: true));

        var result = await _sut.GetCurrentAsync();

        Assert.Equal(new AccountDto(7, "Brano", true), result.Value);
    }

    [Fact]
    public async Task GetSignedInAsync_AcceptsATokenWithTheCurrentStamp()
    {
        var account = TestEntities.Account(7, "Brano", isAdmin: true);
        _accounts.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(account);

        var result = await _sut.GetSignedInAsync(7, account.SecurityStamp);

        Assert.Equal(new AccountDto(7, "Brano", true), result.Value);
    }

    [Theory]
    [InlineData("an-older-stamp")]
    [InlineData("")]
    public async Task GetSignedInAsync_RefusesATokenWithAnotherStamp(string stamp)
    {
        _accounts.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(TestEntities.Account(7, "Brano"));

        var result = await _sut.GetSignedInAsync(7, stamp);

        Assert.Equal(AccountErrors.SignedOut, result.Error);
    }

    [Fact]
    public async Task GetSignedInAsync_ReportsAnAccountThatIsGone()
    {
        var result = await _sut.GetSignedInAsync(99, "stamp");

        Assert.Equal(AccountErrors.UnknownAccount, result.Error);
    }

    [Fact]
    public async Task GetSignedInAsync_DoesNotTreatTheReservedAccountAsALogin()
    {
        var reserved = TestEntities.ReservedAccount();
        _accounts.GetByIdAsync(Account.ReservedId, Arg.Any<CancellationToken>()).Returns(reserved);

        var result = await _sut.GetSignedInAsync(Account.ReservedId, reserved.SecurityStamp);

        Assert.Equal(AccountErrors.UnknownAccount, result.Error);
    }

    [Fact]
    public async Task SignInAsync_UpgradesAWeakHashWithoutEndingOtherSignIns()
    {
        var account = TestEntities.Account(5, "Brano", passwordHash: "old:secret-password");
        _accounts.GetByNameAsync("Brano", Arg.Any<CancellationToken>()).Returns(account);
        var stamp = account.SecurityStamp;

        var result = await _sut.SignInAsync(new SignInRequest { Name = "Brano", Password = "secret-password" });

        Assert.True(result.IsSuccess);
        Assert.Equal(FakePasswordHasher.HashOf("secret-password"), account.PasswordHash);
        Assert.Equal(stamp, account.SecurityStamp);
    }

    private Account Stored(int id, string name, string password)
    {
        var account = TestEntities.Account(id, name, passwordHash: FakePasswordHasher.HashOf(password));
        _accounts.GetByNameAsync(Arg.Is<string>(n => NameKeys.Of(n) == account.NameKey), Arg.Any<CancellationToken>())
            .Returns(account);
        return account;
    }
}

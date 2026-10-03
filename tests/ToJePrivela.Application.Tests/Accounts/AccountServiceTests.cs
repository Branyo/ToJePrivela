using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Accounts;
using ToJePrivela.Application.Accounts.Dtos;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Accounts;

public class AccountServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 3, 18, 0, 0, TimeSpan.Zero);
    private static readonly AccessToken Token = new("issued-token", Now.UtcDateTime.AddHours(12));

    private readonly IExternalIdentityVerifier _google = Substitute.For<IExternalIdentityVerifier>();
    private readonly IExternalIdentityVerifier _facebook = Substitute.For<IExternalIdentityVerifier>();
    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAccessTokenIssuer _tokenIssuer = Substitute.For<IAccessTokenIssuer>();
    private readonly ICurrentAccount _currentAccount = Substitute.For<ICurrentAccount>();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        _google.Provider.Returns(IdentityProvider.Google);
        _google.ClientId.Returns("google-client");
        _facebook.Provider.Returns(IdentityProvider.Facebook);
        _facebook.ClientId.Returns((string?)null);
        _tokenIssuer.Issue(Arg.Any<Account>()).Returns(Token);

        _sut = new AccountService(
            [_facebook, _google],
            _accounts,
            _unitOfWork,
            _tokenIssuer,
            _currentAccount,
            new FixedTimeProvider(Now));
    }

    [Fact]
    public void GetProviders_ListsOnlyTheConfiguredOnes()
    {
        var result = _sut.GetProviders();

        Assert.Equal([new SignInProviderDto("Google", "google-client")], result.Value);
    }

    [Fact]
    public async Task SignInAsync_RegistersANewIdentityAsAnAccountThatIsNoAdmin()
    {
        VerifiedAs(new ExternalIdentity(IdentityProvider.Google, "sub-1", "Brano@Example.com", "Brano"));

        var result = await _sut.SignInAsync(GoogleRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal("issued-token", result.Value.AccessToken);
        Assert.Equal(Token.ExpiresAt, result.Value.ExpiresAt);
        Assert.Equal("Brano", result.Value.Account.DisplayName);
        Assert.Equal("brano@example.com", result.Value.Account.Email);
        Assert.False(result.Value.Account.IsAdmin);
        await _accounts.Received(1).AddAsync(
            Arg.Is<Account>(a => a.ExternalId == "sub-1" && a.Provider == IdentityProvider.Google),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignInAsync_SignsInTheBoundAccountAgain()
    {
        var account = TestEntities.Account(5, "sub-1");
        _accounts.GetBoundAsync(IdentityProvider.Google, "sub-1", Arg.Any<CancellationToken>()).Returns(account);
        VerifiedAs(new ExternalIdentity(IdentityProvider.Google, "sub-1", null, "Renamed"));

        var result = await _sut.SignInAsync(GoogleRequest());

        Assert.Equal(5, result.Value.Account.Id);
        Assert.Equal("Renamed", account.DisplayName);
        Assert.Equal(Now.UtcDateTime, account.LastSignedInAt);
        await _accounts.DidNotReceive().AddAsync(Arg.Any<Account>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignInAsync_BindsTheAdminProvisionedForTheEmail()
    {
        var provisioned = Account.ProvisionAdmin(IdentityProvider.Google, "admin@example.com");
        _accounts.GetProvisionedAsync(IdentityProvider.Google, "admin@example.com", Arg.Any<CancellationToken>())
            .Returns(provisioned);
        VerifiedAs(new ExternalIdentity(IdentityProvider.Google, "sub-admin", "Admin@Example.com", "Admin"));

        var result = await _sut.SignInAsync(GoogleRequest());

        Assert.True(result.Value.Account.IsAdmin);
        Assert.Equal("sub-admin", provisioned.ExternalId);
        _tokenIssuer.Received(1).Issue(provisioned);
    }

    [Fact]
    public async Task SignInAsync_DoesNotLookForAProvisionedAdminWithoutAVerifiedEmail()
    {
        VerifiedAs(new ExternalIdentity(IdentityProvider.Google, "sub-1", null, "Anonymous"));

        var result = await _sut.SignInAsync(GoogleRequest());

        Assert.False(result.Value.Account.IsAdmin);
        await _accounts.DidNotReceive().GetProvisionedAsync(
            Arg.Any<IdentityProvider>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignInAsync_RejectsATokenTheProviderDidNotConfirm()
    {
        _google.VerifyAsync("google-token", Arg.Any<CancellationToken>()).Returns((ExternalIdentity?)null);

        var result = await _sut.SignInAsync(GoogleRequest());

        Assert.Equal("Auth.InvalidToken", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignInAsync_RejectsAProviderThatIsNotConfigured()
    {
        var result = await _sut.SignInAsync(new SignInRequest { Provider = IdentityProvider.Facebook, Token = "fb" });

        Assert.Equal("Auth.ProviderNotConfigured", result.Error.Code);
        await _facebook.DidNotReceive().VerifyAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SignInAsync_ReportsAnUnreachableProviderAsUnavailable()
    {
        _google.VerifyAsync("google-token", Arg.Any<CancellationToken>())
            .ThrowsAsync(new IdentityProviderUnavailableException("down"));

        var result = await _sut.SignInAsync(GoogleRequest());

        Assert.Equal("Auth.ProviderUnavailable", result.Error.Code);
        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
    }

    [Fact]
    public async Task SignInAsync_ValidatesTheRequest()
    {
        var result = await _sut.SignInAsync(new SignInRequest { Provider = null, Token = "" });

        Assert.Equal("Request.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task SignInAsync_PicksUpTheAccountARacingFirstSignInStored()
    {
        var stored = TestEntities.Account(8, "sub-1");
        VerifiedAs(new ExternalIdentity(IdentityProvider.Google, "sub-1", null, "Brano"));
        _accounts.GetBoundAsync(IdentityProvider.Google, "sub-1", Arg.Any<CancellationToken>())
            .Returns((Account?)null, stored);
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => throw new UniqueConstraintException("taken", new Exception()), _ => Task.FromResult(1));

        var result = await _sut.SignInAsync(GoogleRequest());

        Assert.Equal(8, result.Value.Account.Id);
        _unitOfWork.Received(1).DiscardChanges();
    }

    [Fact]
    public async Task GetCurrentAsync_ReturnsTheSignedInAccount()
    {
        _currentAccount.Id.Returns(5);
        _accounts.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns(TestEntities.Account(5, "sub", isAdmin: true));

        var result = await _sut.GetCurrentAsync();

        Assert.Equal(5, result.Value.Id);
        Assert.True(result.Value.IsAdmin);
    }

    [Fact]
    public async Task GetCurrentAsync_RejectsATokenForAnAccountThatIsGone()
    {
        _currentAccount.Id.Returns(5);
        _accounts.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Account?)null);

        var result = await _sut.GetCurrentAsync();

        Assert.Equal("Auth.UnknownAccount", result.Error.Code);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
    }

    private static SignInRequest GoogleRequest() => new() { Provider = IdentityProvider.Google, Token = "google-token" };

    private void VerifiedAs(ExternalIdentity identity) =>
        _google.VerifyAsync("google-token", Arg.Any<CancellationToken>()).Returns(identity);
}

using NSubstitute;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Administration;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Administration;

public class AccountAdministrationServiceTests
{
    private const int AdminId = 1;
    private const int LoginId = 5;
    private const int OtherAdminId = 7;

    private static readonly DateTime Now = new(2026, 10, 24, 18, 0, 0, DateTimeKind.Utc);

    private readonly IAccountRepository _accounts = Substitute.For<IAccountRepository>();
    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();
    private readonly IGameRepository _games = Substitute.For<IGameRepository>();
    private readonly AccountAdministrationService _sut;

    public AccountAdministrationServiceTests()
    {
        _sut = new AccountAdministrationService(_accounts, _players, _games, new FixedCurrentAccount(AdminId));
        _accounts.LoginExistsAsync(LoginId, Arg.Any<CancellationToken>()).Returns(true);
    }

    [Fact]
    public async Task GetAccountsAsync_MapsTheLoginsInTheRepositorysOrder()
    {
        _accounts.GetLoginsAsync(Arg.Any<CancellationToken>()).Returns(
        [
            new LoginSummary(LoginId, "Duri", false, Now, Now.AddHours(1)),
            new LoginSummary(OtherAdminId, "Brano", true, Now, null)
        ]);

        var result = await _sut.GetAccountsAsync();

        Assert.Equal(["Duri", "Brano"], result.Value.Select(a => a.Name));
        Assert.Equal(Now.AddHours(1), result.Value[0].LastSignedInAt);
        Assert.Equal((true, (DateTime?)null), (result.Value[1].IsAdmin, result.Value[1].LastSignedInAt));
    }

    [Theory]
    [InlineData(LoginId, true)]
    [InlineData(OtherAdminId, false)]
    [InlineData(Account.ReservedId, false)]
    public async Task GetAccountsAsync_OffersToDeleteAllButTheAdminsOwnAndTheReservedLogin(int id, bool canDelete)
    {
        _accounts.GetLoginsAsync(Arg.Any<CancellationToken>()).Returns([Login(id, "Duri")]);

        var result = await new AccountAdministrationService(_accounts, _players, _games, new FixedCurrentAccount(OtherAdminId))
            .GetAccountsAsync();

        Assert.Equal(canDelete, Assert.Single(result.Value).CanDelete);
    }

    [Fact]
    public async Task GetAccountAsync_ReadsOneLoginOrSaysItIsNotFound()
    {
        _accounts.GetLoginAsync(LoginId, Arg.Any<CancellationToken>()).Returns(Login(LoginId, "Duri"));

        var found = await _sut.GetAccountAsync(LoginId);
        var missing = await _sut.GetAccountAsync(9);

        Assert.Equal(("Duri", true), (found.Value.Name, found.Value.CanDelete));
        Assert.Equal(AdministrationErrors.AccountNotFound(9), missing.Error);
    }

    [Fact]
    public async Task GetPlayersAsync_ReadsThePlayersOfTheNamedLogin()
    {
        _players.GetAllAsync(LoginId, Arg.Any<CancellationToken>()).Returns([new Player(LoginId, "Anna", "🦊")]);

        var result = await _sut.GetPlayersAsync(LoginId);

        Assert.Equal("Anna", Assert.Single(result.Value).Name);
        // Found players prove the login was there; only an empty read asks again.
        await _accounts.DidNotReceiveWithAnyArgs().LoginExistsAsync(default, default);
    }

    [Fact]
    public async Task Reads_OfAnExistingLoginWithoutData_AreEmptyNotNotFound()
    {
        var players = await _sut.GetPlayersAsync(LoginId);
        var history = await _sut.GetGameHistoryAsync(LoginId, new GameHistoryFilter());

        Assert.Empty(players.Value);
        Assert.Empty(history.Value.Games);
    }

    [Fact]
    public async Task Reads_OfAnUnknownLogin_AreNotFound()
    {
        var players = await _sut.GetPlayersAsync(9);
        var statistics = await _sut.GetPlayerStatisticsAsync(9, 1);
        var history = await _sut.GetGameHistoryAsync(9, new GameHistoryFilter());
        var details = await _sut.GetGameDetailsAsync(9, 1);

        Assert.All(
            new Result[] { players, statistics, history, details },
            result => Assert.Equal(AdministrationErrors.AccountNotFound(9), result.Error));
    }

    [Fact]
    public async Task GetGameHistoryAsync_ValidatesTheFilterBeforeAnythingIsRead()
    {
        var result = await _sut.GetGameHistoryAsync(9, new GameHistoryFilter { Take = 0 });

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Empty(_accounts.ReceivedCalls());
        Assert.Empty(_games.ReceivedCalls());
    }

    [Fact]
    public async Task GetGameDetailsAsync_LooksTheGameUpUnderTheNamedLogin()
    {
        var result = await _sut.GetGameDetailsAsync(LoginId, 3);

        Assert.Equal("Game.NotFound", result.Error.Code);
        await _games.Received(1).GetByIdAsync(LoginId, 3, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAccountAsync_RefusesTheAdminsOwnLogin()
    {
        var result = await _sut.DeleteAccountAsync(AdminId);

        Assert.Equal(AdministrationErrors.CannotDeleteOwnAccount, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _accounts.DidNotReceiveWithAnyArgs().DeleteLoginAsync(default, default);
    }

    [Fact]
    public async Task DeleteAccountAsync_RefusesTheReservedLogin()
    {
        var sut = new AccountAdministrationService(_accounts, _players, _games, new FixedCurrentAccount(OtherAdminId));

        var result = await sut.DeleteAccountAsync(Account.ReservedId);

        Assert.Equal(AdministrationErrors.CannotDeleteReservedAccount, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _accounts.DidNotReceiveWithAnyArgs().DeleteLoginAsync(default, default);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DeleteAccountAsync_DeletesTheLoginOrSaysItIsNotFound(bool deleted)
    {
        _accounts.DeleteLoginAsync(LoginId, Arg.Any<CancellationToken>()).Returns(deleted);

        var result = await _sut.DeleteAccountAsync(LoginId);

        Assert.Equal(deleted, result.IsSuccess);
        Assert.Equal(deleted ? Error.None : AdministrationErrors.AccountNotFound(LoginId), result.Error);
    }

    private static LoginSummary Login(int id, string name) => new(id, name, false, Now, null);
}

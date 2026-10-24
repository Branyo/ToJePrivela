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
        var signedIn = Account.Register("Duri", "hash", Now);
        signedIn.RecordSignIn(Now.AddHours(1));
        _accounts.GetLoginsAsync(Arg.Any<CancellationToken>()).Returns([signedIn, Account.CreateAdmin("Brano", "hash", Now)]);

        var result = await _sut.GetAccountsAsync();

        Assert.Equal(["Duri", "Brano"], result.Value.Select(a => a.Name));
        Assert.Equal(Now.AddHours(1), result.Value[0].LastSignedInAt);
        Assert.Equal((true, (DateTime?)null), (result.Value[1].IsAdmin, result.Value[1].LastSignedInAt));
    }

    [Fact]
    public async Task GetPlayersAsync_ReadsThePlayersOfTheNamedLogin()
    {
        _players.GetAllAsync(LoginId, Arg.Any<CancellationToken>()).Returns([new Player(LoginId, "Anna", "🦊")]);

        var result = await _sut.GetPlayersAsync(LoginId);

        Assert.Equal("Anna", Assert.Single(result.Value).Name);
    }

    [Fact]
    public async Task Reads_OfAnUnknownLogin_AreNotFoundAndReadNothingElse()
    {
        var players = await _sut.GetPlayersAsync(9);
        var statistics = await _sut.GetPlayerStatisticsAsync(9, 1);
        var history = await _sut.GetGameHistoryAsync(9, new GameHistoryFilter());
        var details = await _sut.GetGameDetailsAsync(9, 1);

        Assert.All(
            new Result[] { players, statistics, history, details },
            result => Assert.Equal(AdministrationErrors.AccountNotFound(9), result.Error));
        Assert.Empty(_players.ReceivedCalls());
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
}

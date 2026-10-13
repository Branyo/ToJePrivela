using NSubstitute;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Players;
using ToJePrivela.Application.Statistics;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Statistics;

public class PlayerStatisticsServiceTests
{
    private static readonly DateTime Start = new(2026, 10, 1, 18, 0, 0, DateTimeKind.Utc);

    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();
    private readonly IGameRepository _games = Substitute.For<IGameRepository>();
    private readonly PlayerStatisticsService _sut;

    public PlayerStatisticsServiceTests()
    {
        _sut = new PlayerStatisticsService(_players, _games, new FixedCurrentAccount(TestAccountId));
    }

    [Fact]
    public async Task GetAsync_ReturnsNotFoundForAPlayerOutsideTheLogin()
    {
        _players.GetByIdAsync(TestAccountId, 7, Arg.Any<CancellationToken>()).Returns((Player?)null);

        var result = await _sut.GetAsync(7);

        Assert.Equal(PlayerErrors.NotFound(7), result.Error);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        await _games.DidNotReceiveWithAnyArgs().GetByPlayerAsync(default, default, default);
    }

    [Fact]
    public async Task GetAsync_WorksOutThePlayersStatisticsFromTheirGames()
    {
        _players.GetByIdAsync(TestAccountId, 0, Arg.Any<CancellationToken>()).Returns(new Player(TestAccountId, "Brano", "🦊"));
        var game = new Game(TestAccountId, [0, 1], Start);
        game.AwardDouble(0);
        game.Finish(Start.AddHours(1));
        _games.GetByPlayerAsync(TestAccountId, 0, Arg.Any<CancellationToken>()).Returns([game]);

        var result = await _sut.GetAsync(0);

        Assert.True(result.IsSuccess);
        Assert.Equal("Brano", result.Value.Player.Name);
        Assert.Equal((1, 1, 1, 1), (result.Value.GamesPlayed, result.Value.Doubles, result.Value.FlawlessVictories, result.Value.Reversals));
        Assert.Equal(-1, result.Value.BestGame!.FinalBadPoints);
        Assert.Null(result.Value.LossIndex);
    }
}

using NSubstitute;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Games;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Games;

public class GameServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 22, 18, 0, 0, TimeSpan.Zero);

    private readonly IGameRepository _games = Substitute.For<IGameRepository>();
    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GameService _sut;

    public GameServiceTests()
    {
        _sut = new GameService(_games, _players, _unitOfWork, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task GetAllAsync_MapsPlayerIds()
    {
        _games.GetAllWithDetailsAsync(Arg.Any<CancellationToken>())
            .Returns([TestEntities.Game(1, [1, 2], Now.UtcDateTime)]);

        var result = await _sut.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2], Assert.Single(result.Value).PlayerIds);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFoundForUnknownGame()
    {
        _games.GetWithDetailsAsync(7, Arg.Any<CancellationToken>()).Returns((Game?)null);

        var result = await _sut.GetByIdAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GetDetailsAsync_ReturnsNotFoundForUnknownGame()
    {
        _games.GetWithDetailsAsync(7, Arg.Any<CancellationToken>()).Returns((Game?)null);

        var result = await _sut.GetDetailsAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_StartsTheGameAtTheCurrentTime()
    {
        _players.GetExistingIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns([1, 2]);

        var result = await _sut.CreateAsync(new CreateGameRequest { PlayerIds = [1, 2] });

        Assert.True(result.IsSuccess);
        Assert.Equal(Now.UtcDateTime, result.Value.Started);
        Assert.Null(result.Value.Finished);
        Assert.Equal([1, 2], result.Value.PlayerIds);
        await _games.Received(1).AddAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownPlayers()
    {
        _players.GetExistingIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns([1]);

        var result = await _sut.CreateAsync(new CreateGameRequest { PlayerIds = [1, 99] });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Contains("99", result.Error.Message);
        await _games.DidNotReceive().AddAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReschedulesTheGame()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);

        var finished = Now.UtcDateTime.AddHours(2);
        var result = await _sut.UpdateAsync(1, new UpdateGameRequest { Started = Now.UtcDateTime, Finished = finished });

        Assert.True(result.IsSuccess);
        Assert.Equal(finished, game.Finished);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFoundForUnknownGame()
    {
        _games.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Game?)null);

        var result = await _sut.UpdateAsync(7, new UpdateGameRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheGame()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);

        var result = await _sut.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _games.Received(1).Remove(game);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFoundForUnknownGame()
    {
        _games.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Game?)null);

        var result = await _sut.DeleteAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}

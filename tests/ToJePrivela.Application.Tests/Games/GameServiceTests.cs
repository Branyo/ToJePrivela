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
    private readonly IQuestionRepository _questions = Substitute.For<IQuestionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly GameService _sut;

    public GameServiceTests()
    {
        _sut = new GameService(_games, _players, _questions, _unitOfWork, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task GetAllAsync_MapsPlayerIds()
    {
        _games.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([TestEntities.Game(1, [1, 2], Now.UtcDateTime)]);

        var result = await _sut.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal([1, 2], Assert.Single(result.Value).PlayerIds);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFoundForUnknownGame()
    {
        _games.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Game?)null);

        var result = await _sut.GetByIdAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GetDetailsAsync_ReturnsNotFoundForUnknownGame()
    {
        _games.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Game?)null);

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
    public async Task CreateAsync_ValidatesTheRequestItself()
    {
        var result = await _sut.CreateAsync(new CreateGameRequest { PlayerIds = [1], BadCardLimit = 50 });

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Request.Invalid", result.Error.Code);
        await _players.DidNotReceive().GetExistingIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>());
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
    public async Task UpdateAsync_RequiresTheStart()
    {
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestEntities.Game(1, [1, 2], Now.UtcDateTime));

        var result = await _sut.UpdateAsync(1, new UpdateGameRequest { Finished = Now.UtcDateTime.AddHours(1) });

        Assert.Equal("Game.StartRequired", result.Error.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsConflictWhenReopeningAFinishedGame()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime);
        game.Finish(Now.UtcDateTime.AddHours(1));
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);

        var result = await _sut.UpdateAsync(1, new UpdateGameRequest { Started = Now.UtcDateTime });

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Game.CannotReopen", result.Error.Code);
        Assert.True(game.IsFinished);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_RejectsAnEndBeforeTheStart()
    {
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestEntities.Game(1, [1, 2], Now.UtcDateTime));

        var result = await _sut.UpdateAsync(1, new UpdateGameRequest { Started = Now.UtcDateTime, Finished = Now.UtcDateTime.AddHours(-1) });

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Game.EndsBeforeStart", result.Error.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
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

    [Fact]
    public async Task CreateAsync_UsesTheRequestedBadCardLimit()
    {
        _players.GetExistingIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns([1, 2]);

        var result = await _sut.CreateAsync(new CreateGameRequest { PlayerIds = [1, 2], BadCardLimit = 5 });

        Assert.Equal(5, result.Value.BadCardLimit);
    }

    [Fact]
    public async Task CreateAsync_TakesBadPointsFromTheQuestionUnlessChooserIsRequested()
    {
        _players.GetExistingIdsAsync(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>()).Returns([1, 2]);

        var byDefault = await _sut.CreateAsync(new CreateGameRequest { PlayerIds = [1, 2] });
        var chooser = await _sut.CreateAsync(new CreateGameRequest { PlayerIds = [1, 2], BadPointsMode = BadPointsMode.Chooser });

        Assert.Equal("Question", byDefault.Value.BadPointsMode);
        Assert.Equal("Chooser", chooser.Value.BadPointsMode);
    }

    [Fact]
    public async Task AwardBadCardAsync_InAChooserGameGivesTheCardWorthTheChosenBadPoints()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime, badPointsMode: BadPointsMode.Chooser);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);
        _questions.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(TestEntities.Question(10, "How many wheels?", "4", TestEntities.Category(1, "Cars"), badPoints: 4));

        var result = await _sut.AwardBadCardAsync(1, new AwardBadCardRequest { PlayerId = 2, QuestionId = 10, BadPoints = 1 });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Players.Single(p => p.PlayerId == 2).BadPoints);
    }

    [Fact]
    public async Task AwardBadCardAsync_InAChooserGameRequiresTheBadPoints()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime, badPointsMode: BadPointsMode.Chooser);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);

        var result = await _sut.AwardBadCardAsync(1, new AwardBadCardRequest { PlayerId = 2, QuestionId = 10 });

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Game.BadPointsRequired", result.Error.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AwardBadCardAsync_InAQuestionGameRejectsClientBadPoints()
    {
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestEntities.Game(1, [1, 2], Now.UtcDateTime));

        var result = await _sut.AwardBadCardAsync(1, new AwardBadCardRequest { PlayerId = 2, QuestionId = 10, BadPoints = 5 });

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Game.BadPointsNotAllowed", result.Error.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AwardBadCardAsync_GivesTheCardWorthTheQuestionsBadPoints()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);
        _questions.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(TestEntities.Question(10, "How many wheels?", "4", TestEntities.Category(1, "Cars"), badPoints: 4));

        var result = await _sut.AwardBadCardAsync(1, new AwardBadCardRequest { PlayerId = 2, QuestionId = 10 });

        Assert.True(result.IsSuccess);
        var player = result.Value.Players.Single(p => p.PlayerId == 2);
        Assert.Equal(1, player.BadCards);
        Assert.Equal(4, player.BadPoints);
        Assert.Null(result.Value.Finished);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AwardBadCardAsync_FinishesTheGameAtTheLimit()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime, badCardLimit: 2);
        game.AwardBadCard(1, TestEntities.Question(9, "How many doors?", "5", TestEntities.Category(1, "Cars"), badPoints: 2), null, Now.UtcDateTime);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);
        _questions.GetByIdAsync(10, Arg.Any<CancellationToken>())
            .Returns(TestEntities.Question(10, "How many wheels?", "4", TestEntities.Category(1, "Cars")));

        var result = await _sut.AwardBadCardAsync(1, new AwardBadCardRequest { PlayerId = 1, QuestionId = 10 });

        Assert.Equal(Now.UtcDateTime, result.Value.Finished);
    }

    [Fact]
    public async Task AwardBadCardAsync_ReturnsNotFoundForUnknownGame()
    {
        _games.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Game?)null);

        var result = await _sut.AwardBadCardAsync(7, new AwardBadCardRequest { PlayerId = 1, QuestionId = 10 });

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task AwardBadCardAsync_ReturnsConflictForAFinishedGame()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime);
        game.Finish(Now.UtcDateTime);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);

        var result = await _sut.AwardBadCardAsync(1, new AwardBadCardRequest { PlayerId = 1, QuestionId = 10 });

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AwardBadCardAsync_RejectsAPlayerOutsideTheGame()
    {
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestEntities.Game(1, [1, 2], Now.UtcDateTime));

        var result = await _sut.AwardBadCardAsync(1, new AwardBadCardRequest { PlayerId = 9, QuestionId = 10 });

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Game.PlayerNotInGame", result.Error.Code);
    }

    [Fact]
    public async Task AwardBadCardAsync_RejectsAnUnknownQuestion()
    {
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestEntities.Game(1, [1, 2], Now.UtcDateTime));
        _questions.GetByIdAsync(10, Arg.Any<CancellationToken>()).Returns((Question?)null);

        var result = await _sut.AwardBadCardAsync(1, new AwardBadCardRequest { PlayerId = 1, QuestionId = 10 });

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Game.UnknownQuestion", result.Error.Code);
    }

    [Fact]
    public async Task AwardDoubleAsync_TakesOneBadPointOffTheFinalScore()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime);
        game.AwardBadCard(2, TestEntities.Question(9, "How many doors?", "5", TestEntities.Category(1, "Cars"), badPoints: 4), null, Now.UtcDateTime);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);

        var result = await _sut.AwardDoubleAsync(1, new AwardDoubleRequest { PlayerId = 2 });

        Assert.True(result.IsSuccess);
        var player = result.Value.Players.Single(p => p.PlayerId == 2);
        Assert.Equal((1, 4, 1, 3), (player.BadCards, player.BadPoints, player.Doubles, player.FinalBadPoints));
        Assert.Null(result.Value.Finished);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AwardDoubleAsync_ReturnsNotFoundForUnknownGame()
    {
        _games.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Game?)null);

        var result = await _sut.AwardDoubleAsync(7, new AwardDoubleRequest { PlayerId = 1 });

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task AwardDoubleAsync_ReturnsConflictForAFinishedGame()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime);
        game.Finish(Now.UtcDateTime);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);

        var result = await _sut.AwardDoubleAsync(1, new AwardDoubleRequest { PlayerId = 1 });

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AwardDoubleAsync_RejectsAPlayerOutsideTheGame()
    {
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestEntities.Game(1, [1, 2], Now.UtcDateTime));

        var result = await _sut.AwardDoubleAsync(1, new AwardDoubleRequest { PlayerId = 9 });

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal("Game.PlayerNotInGame", result.Error.Code);
    }

    [Fact]
    public async Task RemoveDoubleAsync_TakesBackOneDouble()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime);
        game.AwardDouble(2);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);

        var result = await _sut.RemoveDoubleAsync(1, 2);

        Assert.True(result.IsSuccess);
        var player = result.Value.Players.Single(p => p.PlayerId == 2);
        Assert.Equal((0, 0), (player.Doubles, player.FinalBadPoints));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveDoubleAsync_ReturnsConflictWhenThePlayerHasNoDouble()
    {
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestEntities.Game(1, [1, 2], Now.UtcDateTime));

        var result = await _sut.RemoveDoubleAsync(1, 2);

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Game.NoDoubleToRemove", result.Error.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveDoubleAsync_RejectsAPlayerOutsideTheGame()
    {
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestEntities.Game(1, [1, 2], Now.UtcDateTime));

        var result = await _sut.RemoveDoubleAsync(1, 9);

        Assert.Equal("Game.PlayerNotInGame", result.Error.Code);
    }

    [Fact]
    public async Task RemoveDoubleAsync_ReturnsConflictForAFinishedGame()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime);
        game.AwardDouble(1);
        game.Finish(Now.UtcDateTime);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);

        var result = await _sut.RemoveDoubleAsync(1, 1);

        Assert.Equal("Game.AlreadyFinished", result.Error.Code);
    }

    [Fact]
    public async Task FinishAsync_FinishesTheGameNow()
    {
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestEntities.Game(1, [1, 2], Now.UtcDateTime));

        var result = await _sut.FinishAsync(1);

        Assert.Equal(Now.UtcDateTime, result.Value.Finished);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinishAsync_ReturnsConflictForAFinishedGame()
    {
        var game = TestEntities.Game(1, [1, 2], Now.UtcDateTime);
        game.Finish(Now.UtcDateTime);
        _games.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(game);

        var result = await _sut.FinishAsync(1);

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }
}

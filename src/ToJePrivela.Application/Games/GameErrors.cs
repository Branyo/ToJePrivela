using ToJePrivela.Application.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Games;

public static class GameErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("Game.NotFound", $"Game with id {id} was not found.");

    public static Error UnknownPlayers(IEnumerable<int> playerIds) =>
        Error.Validation("Game.UnknownPlayers", $"One or more player ids are invalid: {string.Join(", ", playerIds)}.");

    public static Error AlreadyFinished(int id) =>
        Error.Conflict("Game.AlreadyFinished", $"Game with id {id} is already finished.");

    public static Error PlayerNotInGame(int gameId, int playerId) =>
        Error.Validation("Game.PlayerNotInGame", $"Player {playerId} does not play in game {gameId}.");

    public static Error NoDoubleToRemove(int gameId, int playerId) =>
        Error.Conflict("Game.NoDoubleToRemove", $"Player {playerId} has no double to take back in game {gameId}.");

    public static Error BadPointsNotAllowed(int gameId) =>
        Error.Validation(
            "Game.BadPointsNotAllowed",
            $"Game {gameId} takes bad points from the question, so they cannot be sent with the card.");

    public static Error BadPointsRequired(int gameId) =>
        Error.Validation(
            "Game.BadPointsRequired",
            $"Game {gameId} has its bad points set before each question, so the card needs them.");

    public static Error StartRequired(int gameId) =>
        Error.Validation("Game.StartRequired", $"Game {gameId} must keep its start time.");

    public static Error CannotReopen(int gameId) =>
        Error.Conflict("Game.CannotReopen", $"Game {gameId} is finished and cannot be reopened.");

    public static Error EndsBeforeStart(int gameId) =>
        Error.Validation("Game.EndsBeforeStart", $"Game {gameId} cannot be finished before it started.");

    /// <summary>The error for a rule the domain reported; the rule itself lives in <see cref="Game"/>.</summary>
    public static Error From(GameRuleViolation violation, int gameId, int? playerId = null) => violation switch
    {
        GameRuleViolation.AlreadyFinished => AlreadyFinished(gameId),
        GameRuleViolation.PlayerNotInGame => PlayerNotInGame(gameId, playerId ?? 0),
        GameRuleViolation.NoDoubleToRemove => NoDoubleToRemove(gameId, playerId ?? 0),
        GameRuleViolation.ChosenBadPointsRequired => BadPointsRequired(gameId),
        GameRuleViolation.ChosenBadPointsNotAllowed => BadPointsNotAllowed(gameId),
        GameRuleViolation.CannotReopen => CannotReopen(gameId),
        GameRuleViolation.EndsBeforeStart => EndsBeforeStart(gameId),
        _ => throw new ArgumentOutOfRangeException(nameof(violation), violation, null)
    };

    public static Error UnknownQuestion(int questionId) =>
        Error.Validation("Game.UnknownQuestion", $"Question with id {questionId} does not exist.");
}

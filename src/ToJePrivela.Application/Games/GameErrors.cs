using ToJePrivela.Application.Common;

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

    public static Error UnknownQuestion(int questionId) =>
        Error.Validation("Game.UnknownQuestion", $"Question with id {questionId} does not exist.");
}

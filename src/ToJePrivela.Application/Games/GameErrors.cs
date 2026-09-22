using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.Games;

public static class GameErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("Game.NotFound", $"Game with id {id} was not found.");

    public static Error UnknownPlayers(IEnumerable<int> playerIds) =>
        Error.Validation("Game.UnknownPlayers", $"One or more player ids are invalid: {string.Join(", ", playerIds)}.");
}

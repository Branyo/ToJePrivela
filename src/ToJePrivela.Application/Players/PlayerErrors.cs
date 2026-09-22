using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.Players;

public static class PlayerErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("Player.NotFound", $"Player with id {id} was not found.");

    public static Error NameTaken(string name) =>
        Error.Conflict("Player.NameTaken", $"Player with name '{name}' already exists.");
}

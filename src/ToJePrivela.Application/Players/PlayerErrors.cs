using ToJePrivela.Application.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Players;

public static class PlayerErrors
{
    public static Error NotFound(int id) =>
        Error.NotFound("Player.NotFound", $"Player with id {id} was not found.");

    public static readonly Error LimitReached =
        Error.Conflict("Player.LimitReached", $"A login can have at most {Account.MaxPlayers} players; delete one first.");

    public static Error NameTaken(string name) =>
        Error.Conflict("Player.NameTaken", $"You already have a player named '{name}'.");
}

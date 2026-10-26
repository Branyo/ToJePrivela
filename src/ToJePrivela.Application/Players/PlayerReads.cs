using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Players.Mapping;

namespace ToJePrivela.Application.Players;

/// <summary>
/// One account's players, shared by the signed-in login (<see cref="PlayerService"/>) and an admin looking at a login
/// (<c>AccountAdministrationService</c>), so both list them alike.
/// </summary>
internal static class PlayerReads
{
    public static async Task<Result<IReadOnlyList<PlayerDto>>> GetAllAsync(
        IPlayerRepository players,
        int accountId,
        CancellationToken cancellationToken) =>
        Result.Success(PlayerMapper.ToDtos(await players.GetAllAsync(accountId, cancellationToken)));
}

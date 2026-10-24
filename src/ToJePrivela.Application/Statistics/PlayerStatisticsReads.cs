using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Players;
using ToJePrivela.Application.Statistics.Dtos;
using ToJePrivela.Application.Statistics.Mapping;
using ToJePrivela.Domain.Statistics;

namespace ToJePrivela.Application.Statistics;

/// <summary>
/// One account's player statistics, shared by the signed-in login (<see cref="PlayerStatisticsService"/>) and an admin
/// looking at a login (<c>AccountAdministrationService</c>).
/// </summary>
internal static class PlayerStatisticsReads
{
    public static async Task<Result<PlayerStatisticsDto>> GetAsync(
        IPlayerRepository players,
        IGameRepository games,
        int accountId,
        int playerId,
        CancellationToken cancellationToken)
    {
        var player = await players.GetByIdAsync(accountId, playerId, cancellationToken);

        if (player is null)
        {
            return Result.Failure<PlayerStatisticsDto>(PlayerErrors.NotFound(playerId));
        }

        var counted = await games.GetCountedByPlayerAsync(accountId, playerId, cancellationToken);

        return Result.Success(PlayerStatisticsMapper.ToDto(player, PlayerStatistics.For(playerId, counted)));
    }
}

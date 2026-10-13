using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Players;
using ToJePrivela.Application.Statistics.Dtos;
using ToJePrivela.Application.Statistics.Mapping;
using ToJePrivela.Domain.Statistics;

namespace ToJePrivela.Application.Statistics;

/// <summary>Works out the statistics on every request from the signed-in account's games; nothing is stored.</summary>
public sealed class PlayerStatisticsService : IPlayerStatisticsService
{
    private readonly IPlayerRepository _players;
    private readonly IGameRepository _games;
    private readonly ICurrentAccount _currentAccount;

    public PlayerStatisticsService(IPlayerRepository players, IGameRepository games, ICurrentAccount currentAccount)
    {
        _players = players;
        _games = games;
        _currentAccount = currentAccount;
    }

    public async Task<Result<PlayerStatisticsDto>> GetAsync(int playerId, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetByIdAsync(_currentAccount.Id, playerId, cancellationToken);

        if (player is null)
        {
            return Result.Failure<PlayerStatisticsDto>(PlayerErrors.NotFound(playerId));
        }

        var games = await _games.GetByPlayerAsync(_currentAccount.Id, playerId, cancellationToken);

        return Result.Success(PlayerStatisticsMapper.ToDto(player, PlayerStatistics.For(playerId, games)));
    }
}

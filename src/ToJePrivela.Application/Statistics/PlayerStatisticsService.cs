using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Statistics.Dtos;

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

    public Task<Result<PlayerStatisticsDto>> GetAsync(int playerId, CancellationToken cancellationToken = default) =>
        PlayerStatisticsReads.GetAsync(_players, _games, _currentAccount.Id, playerId, cancellationToken);
}

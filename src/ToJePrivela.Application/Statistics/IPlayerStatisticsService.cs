using ToJePrivela.Application.Common;
using ToJePrivela.Application.Statistics.Dtos;

namespace ToJePrivela.Application.Statistics;

public interface IPlayerStatisticsService
{
    /// <summary>The player's record over their finished, not cancelled games; 404 for another login's player.</summary>
    Task<Result<PlayerStatisticsDto>> GetAsync(int playerId, CancellationToken cancellationToken = default);
}

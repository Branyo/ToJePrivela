using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Games.Mapping;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Application.Games;

/// <summary>
/// Reads of one account's games, shared by the signed-in login's own use cases (<see cref="GameService"/>) and an
/// admin looking at a login (<c>AccountAdministrationService</c>), so both list and show games alike.
/// </summary>
internal static class GameReads
{
    public static async Task<Result<GameHistoryDto>> GetHistoryAsync(
        IGameRepository games,
        int accountId,
        GameHistoryFilter filter,
        CancellationToken cancellationToken)
    {
        if (RequestValidator.Validate(filter) is { } invalid)
        {
            return Result.Failure<GameHistoryDto>(invalid);
        }

        var after = filter.BeforeId is not int beforeId ? null
            // The start the client listed the game at, so a game moved meanwhile neither skips nor repeats the others.
            : filter.Before is DateTime before ? new GamePosition(UtcTime.Normalize(before), beforeId)
            // Without one, where the game stands now. A game deleted meanwhile is taken to have had no start: the client
            // sends "before" whenever the game it listed had one.
            : await games.GetPositionAsync(accountId, beforeId, cancellationToken) ?? new GamePosition(null, beforeId);

        // One game past the page tells whether older ones follow, without counting them all.
        var page = await games.GetRecentAsync(accountId, filter.Status, after, filter.Take + 1, cancellationToken);

        return Result.Success(new GameHistoryDto(
            page.Take(filter.Take).Select(GameMapper.ToDetailsDto).ToList(),
            page.Count > filter.Take));
    }

    public static async Task<Result<GameDetailsDto>> GetDetailsAsync(
        IGameRepository games,
        int accountId,
        int id,
        CancellationToken cancellationToken)
    {
        var game = await games.GetByIdAsync(accountId, id, cancellationToken);

        return game is null
            ? Result.Failure<GameDetailsDto>(GameErrors.NotFound(id))
            : Result.Success(GameMapper.ToDetailsDto(game));
    }
}

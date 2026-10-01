using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Games.Mapping;

public static class GameMapper
{
    public static GameDto ToDto(Game game) => new(
        game.Id,
        game.Started,
        game.Finished,
        game.BadCardLimit,
        game.BadPointsMode.ToString(),
        game.IsCancelled,
        game.GamePlayers.OrderBy(SeatOrder).Select(gp => gp.PlayerId).ToList());

    public static IReadOnlyList<GameDto> ToDtos(IEnumerable<Game> games) => games.Select(ToDto).ToList();

    /// <summary>Players stay in seat order (player id, unknown players last); each carries the rank the game gives them.</summary>
    public static GameDetailsDto ToDetailsDto(Game game) => new(
        game.Id,
        game.Started,
        game.Finished,
        game.BadCardLimit,
        game.BadPointsMode.ToString(),
        game.IsCancelled,
        game.Standings()
            .OrderBy(standing => SeatOrder(standing.Player))
            .Select(ToDto)
            .ToList());

    private static GamePlayerDto ToDto(GameStanding standing) => new(
        standing.Player.PlayerId,
        standing.Player.IsUnknownPlayer ? null : standing.Player.Player?.Name ?? string.Empty,
        standing.Player.IsUnknownPlayer ? null : standing.Player.Player?.Avatar ?? string.Empty,
        standing.Player.BadPoints,
        standing.Player.BadCards,
        standing.Player.Doubles,
        standing.Player.FinalBadPoints,
        standing.Rank,
        standing.IsLoser);

    private static int SeatOrder(GamePlayer gamePlayer) => gamePlayer.PlayerId ?? int.MaxValue;
}

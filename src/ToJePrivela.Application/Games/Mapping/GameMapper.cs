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
        game.GamePlayers.OrderBy(gp => gp.Seat).ThenBy(gp => gp.Id).Select(gp => gp.PlayerId).ToList());

    public static IReadOnlyList<GameDto> ToDtos(IEnumerable<Game> games) => games.Select(ToDto).ToList();

    /// <summary>Players stay in the order of play (<c>GamePlayer.Seat</c>); each carries the rank the game gives them.</summary>
    public static GameDetailsDto ToDetailsDto(Game game) => new(
        game.Id,
        game.Started,
        game.Finished,
        game.BadCardLimit,
        game.BadPointsMode.ToString(),
        game.IsCancelled,
        game.HasResult,
        game.Standings()
            .OrderBy(standing => standing.Player.Seat)
            .ThenBy(standing => standing.Player.Id)
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
        standing.IsLoser,
        standing.Player.TooMuchCalls,
        standing.Player.FalseAlarms,
        standing.Player.CorrectCalls,
        standing.Player.Caught);
}

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
        game.PlayersInSeatOrder.Select(gp => gp.PlayerId).ToList());

    public static IReadOnlyList<GameDto> ToDtos(IEnumerable<Game> games) => games.Select(ToDto).ToList();

    /// <summary>
    /// Players stay in the order of play (<c>Game.PlayersInSeatOrder</c>); each carries the rank the game gives them and
    /// the player before them (<c>Game.PreviousPlayerOf</c>).
    /// </summary>
    public static GameDetailsDto ToDetailsDto(Game game)
    {
        var standings = game.Standings().ToDictionary(standing => standing.Player);

        return new(
            game.Id,
            game.Started,
            game.Finished,
            game.BadCardLimit,
            game.BadPointsMode.ToString(),
            game.IsCancelled,
            game.HasResult,
            game.StarterId,
            game.PlayersInSeatOrder.Select(gp => ToDto(standings[gp], game)).ToList());
    }

    private static GamePlayerDto ToDto(GameStanding standing, Game game) => new(
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
        standing.Player.Caught,
        standing.Player.PlayerId is { } playerId ? game.PreviousPlayerOf(playerId) : null);
}

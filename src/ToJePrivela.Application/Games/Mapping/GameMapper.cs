using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Games.Mapping;

public static class GameMapper
{
    public static GameDto ToDto(Game game) => new(
        game.Id,
        game.Started,
        game.Finished,
        game.GamePlayers.Select(gp => gp.PlayerId).ToList());

    public static IReadOnlyList<GameDto> ToDtos(IEnumerable<Game> games) => games.Select(ToDto).ToList();

    public static GameDetailsDto ToDetailsDto(Game game) => new(
        game.Id,
        game.Started,
        game.Finished,
        game.GamePlayers
            .Select(gp => new GamePlayerDto(gp.PlayerId, gp.Player?.Name ?? string.Empty, gp.BadPoints))
            .ToList());
}

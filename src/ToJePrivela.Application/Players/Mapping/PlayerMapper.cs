using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Players.Mapping;

public static class PlayerMapper
{
    public static PlayerDto ToDto(Player player) => new(player.Id, player.Name);

    public static IReadOnlyList<PlayerDto> ToDtos(IEnumerable<Player> players) =>
        players.Select(ToDto).ToList();

    public static Player ToEntity(CreatePlayerRequest request) => new(request.Name);
}

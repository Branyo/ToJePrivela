using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Games.Dtos;

public sealed class CreateGameRequest
{
    [Required]
    [MinLength(Game.MinPlayers, ErrorMessage = "Game must have at least 2 players.")]
    [MaxLength(Game.MaxPlayers, ErrorMessage = "Game can have maximum of 10 players.")]
    public IList<int> PlayerIds { get; init; } = [];
}

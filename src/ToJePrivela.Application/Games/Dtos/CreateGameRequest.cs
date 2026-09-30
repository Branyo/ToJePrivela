using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Games.Dtos;

public sealed class CreateGameRequest
{
    [Required]
    [MinLength(Game.MinPlayers, ErrorMessage = "Game must have at least 2 players.")]
    [MaxLength(Game.MaxPlayers, ErrorMessage = "Game can have maximum of 12 players.")]
    public IList<int> PlayerIds { get; init; } = [];

    /// <summary>Bad cards that end the game; <see cref="Game.DefaultBadCardLimit"/> when left out.</summary>
    [Range(Game.MinBadCardLimit, Game.MaxBadCardLimit, ErrorMessage = "Bad card limit should be between 2 and 10.")]
    public int? BadCardLimit { get; init; }

    /// <summary>
    /// <c>"Question"</c> (the question's stored bad points) or <c>"Chooser"</c> (set before each question);
    /// <see cref="BadPointsMode.Question"/> when left out.
    /// </summary>
    [EnumDataType(typeof(BadPointsMode), ErrorMessage = "Bad points mode should be Question or Chooser.")]
    [JsonConverter(typeof(JsonStringEnumConverter<BadPointsMode>))]
    public BadPointsMode? BadPointsMode { get; init; }
}

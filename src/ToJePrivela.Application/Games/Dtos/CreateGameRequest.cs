using System.ComponentModel.DataAnnotations;
using ToJePrivela.Application.Common.Validation;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Games.Dtos;

public sealed class CreateGameRequest
{
    [Required]
    [DistinctCount(Game.MinPlayers, Game.MaxPlayers, ErrorMessage = "A game needs from {1} to {2} different players.")]
    public IList<int> PlayerIds { get; init; } = [];

    /// <summary>Bad cards that end the game; <see cref="Game.DefaultBadCardLimit"/> when left out.</summary>
    [Range(Game.MinBadCardLimit, Game.MaxBadCardLimit, ErrorMessage = "Bad card limit should be between {1} and {2}.")]
    public int? BadCardLimit { get; init; }

    /// <summary>
    /// <c>"Question"</c> (the question's stored bad points) or <c>"Chooser"</c> (set before each question);
    /// <see cref="BadPointsMode.Question"/> when left out.
    /// </summary>
    [EnumDataType(typeof(BadPointsMode), ErrorMessage = "Bad points mode should be Question or Chooser.")]
    public BadPointsMode? BadPointsMode { get; init; }
}

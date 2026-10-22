using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Application.Games.Dtos;

/// <summary>
/// One page of the signed-in account's games, newest first: the first <see cref="Take"/> games, or with
/// <see cref="BeforeId"/> the <see cref="Take"/> games listed after that one. A page continues from the last game
/// listed, not from a count, so games started, finished or deleted meanwhile neither repeat nor skip one.
/// </summary>
public sealed class GameHistoryFilter : IValidatableObject
{
    public const int DefaultTake = 20;
    public const int MaxTake = 50;

    [EnumDataType(typeof(GameHistoryStatus), ErrorMessage = "Status should be All, Running or Finished.")]
    public GameHistoryStatus Status { get; init; } = GameHistoryStatus.All;

    /// <summary>The last listed game's <c>started</c>; left out when that game has none.</summary>
    public DateTime? Before { get; init; }

    /// <summary>The last listed game's id; the page continues after it.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "Before id should be positive.")]
    public int? BeforeId { get; init; }

    [Range(1, MaxTake, ErrorMessage = "Take should be between {1} and {2}.")]
    public int Take { get; init; } = DefaultTake;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Before is not null && BeforeId is null)
        {
            yield return new ValidationResult("Before needs the before id of the same game.", [nameof(BeforeId)]);
        }
    }
}

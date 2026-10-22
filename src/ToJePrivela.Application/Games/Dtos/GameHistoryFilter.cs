using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Application.Games.Dtos;

/// <summary>One page of the signed-in account's games, newest first: <see cref="Take"/> games after the first <see cref="Skip"/>.</summary>
public sealed class GameHistoryFilter
{
    public const int DefaultTake = 20;
    public const int MaxTake = 50;

    [EnumDataType(typeof(GameHistoryStatus), ErrorMessage = "Status should be All, Running or Finished.")]
    public GameHistoryStatus Status { get; init; } = GameHistoryStatus.All;

    [Range(0, int.MaxValue, ErrorMessage = "Skip should not be negative.")]
    public int Skip { get; init; }

    [Range(1, MaxTake, ErrorMessage = "Take should be between {1} and {2}.")]
    public int Take { get; init; } = DefaultTake;
}

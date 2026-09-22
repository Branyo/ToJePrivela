using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Application.Questions.Dtos;

public sealed class GenerateQuestionsRequest
{
    public const int MaxCount = 25;

    public string? Category { get; init; }

    [Range(1, MaxCount, ErrorMessage = "Count should be between 1 and 25.")]
    public int Count { get; init; } = 10;

    public string? Language { get; init; }
}

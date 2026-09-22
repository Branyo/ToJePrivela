using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Questions.Dtos;

public sealed class QuestionFilter
{
    public string? Category { get; init; }

    [Range(Question.MinDifficulty, Question.MaxDifficulty, ErrorMessage = "Difficulty should be between 1 and 5.")]
    public int? Difficulty { get; init; }
}

using System.ComponentModel.DataAnnotations;
using ToJePrivela.Application.QuestionGeneration;

namespace ToJePrivela.Application.QuestionCategories.Dtos;

public sealed class GenerateAiQuestionsRequest
{
    [Required(ErrorMessage = "Question count is required.")]
    [Range(1, QuestionGenerationOptions.MaxCount, ErrorMessage = "Question count should be between 1 and 200.")]
    public int? Count { get; init; }
}

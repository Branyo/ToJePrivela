using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Application.QuestionImport.Dtos;

public sealed class ImportQuestionsRequest
{
    /// <summary>The JSON array as text, in the format the AI generator answers with (a file's content or a pasted reply).</summary>
    [Required(ErrorMessage = "The JSON text with the questions is required.")]
    [StringLength(QuestionImporter.MaxJsonLength, ErrorMessage = "The JSON text should have at most {1} characters.")]
    public string Json { get; init; } = default!;
}

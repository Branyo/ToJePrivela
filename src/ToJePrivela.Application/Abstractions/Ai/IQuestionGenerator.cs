namespace ToJePrivela.Application.Abstractions.Ai;

/// <summary>
/// Port implemented by the AI layer. Each method is a single call to the provider; retries, batching,
/// duplicate handling and deciding which questions are usable belong to the caller. Every question
/// comes in Slovak and in English. Both methods throw
/// <see cref="QuestionGeneratorUnavailableException"/> when the provider cannot be used.
/// </summary>
public interface IQuestionGenerator
{
    /// <summary>
    /// Splits a category (its English name) into distinct subtopics, so parallel requests do not repeat each other.
    /// </summary>
    Task<IReadOnlyList<string>> GenerateSubtopicsAsync(
        string category,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>Returns every well-formed question the provider sent, trimmed but not otherwise judged.</summary>
    Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(
        QuestionGenerationRequest request,
        CancellationToken cancellationToken = default);
}

/// <param name="Category">The category's English name.</param>
/// <param name="ExcludedQuestions">Questions the model is asked not to repeat.</param>
public sealed record QuestionGenerationRequest(
    string Category,
    int Count,
    string? Subtopic = null,
    IReadOnlyList<string>? ExcludedQuestions = null);

public sealed record GeneratedQuestion(string TextSk, string TextEn, string Answer);

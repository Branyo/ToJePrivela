namespace ToJePrivela.Application.Abstractions.Ai;

/// <summary>
/// Port implemented by the AI layer. Each method is a single call to the provider; retries, batching,
/// duplicate handling and deciding which questions are usable belong to the caller. The question
/// language is the AI layer's own setting.
/// </summary>
public interface IQuestionGenerator
{
    /// <summary>Splits a category into distinct subtopics, so parallel requests do not repeat each other.</summary>
    Task<IReadOnlyList<string>> GenerateSubtopicsAsync(
        string category,
        int count,
        CancellationToken cancellationToken = default);

    /// <summary>Returns every well-formed question the provider sent, trimmed but not otherwise judged.</summary>
    Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(
        QuestionGenerationRequest request,
        CancellationToken cancellationToken = default);
}

/// <param name="ExcludedQuestions">Questions the model is asked not to repeat.</param>
public sealed record QuestionGenerationRequest(
    string Category,
    int Count,
    string? Subtopic = null,
    IReadOnlyList<string>? ExcludedQuestions = null);

public sealed record GeneratedQuestion(string Text, string Answer);

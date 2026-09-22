namespace ToJePrivela.Application.Abstractions.Ai;

/// <summary>Port implemented by the AI layer.</summary>
public interface IQuestionGenerator
{
    Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(
        QuestionGenerationRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record QuestionGenerationRequest(string? Category, int Count, string? Language = null, int? Difficulty = null);

public sealed record GeneratedQuestion(string Text, string Answer, string Category, int Difficulty);

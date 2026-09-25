using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionGeneration;

/// <param name="Questions">New, not yet stored AI questions.</param>
/// <param name="Discarded">Items the provider returned that were duplicates or over the requested count.</param>
public sealed record QuestionGenerationResult(IReadOnlyList<Question> Questions, int Requested, int Discarded)
{
    public static QuestionGenerationResult Nothing { get; } = new([], 0, 0);

    public int Created => Questions.Count;

    /// <summary>Something was asked for and nothing usable came back.</summary>
    public bool Failed => Requested > 0 && Created == 0;
}

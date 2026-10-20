using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionImport;

/// <summary>
/// Turns JSON in the AI generator's format into questions of a category, judged like generated ones. Reading and
/// judging are separate steps, so a use case can refuse bad JSON before it spends anything else (such as a translation).
/// </summary>
public interface IQuestionImporter
{
    /// <returns>One entry per array item (null for an item that is not a question), or why the text cannot be imported.</returns>
    Result<IReadOnlyList<GeneratedQuestion?>> Read(string json);

    /// <summary>
    /// Builds unsaved AI questions from the usable items; duplicates (against the category's stored questions, when it
    /// is saved already, and each other) and unusable items are reported instead.
    /// </summary>
    Task<QuestionImportOutcome> ToQuestionsAsync(
        QuestionCategory category,
        IReadOnlyList<GeneratedQuestion?> items,
        CancellationToken cancellationToken = default);
}

/// <param name="Received">Items in the JSON array.</param>
/// <param name="Questions">New, not yet stored questions.</param>
/// <param name="Rejected">The items left out, in array order.</param>
public sealed record QuestionImportOutcome(int Received, IReadOnlyList<Question> Questions, IReadOnlyList<RejectedQuestion> Rejected);

/// <param name="Position">The item's 1-based position in the JSON array.</param>
public sealed record RejectedQuestion(int Position, QuestionRejection Reason);

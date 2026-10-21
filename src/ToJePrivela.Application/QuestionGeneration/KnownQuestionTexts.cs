using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionGeneration;

/// <summary>
/// The Slovak texts new AI questions of a category must not repeat: the category's stored questions and the ones taken
/// so far, compared as <see cref="QuestionTextNormalizer"/> normalizes them. Shared by generation and import, so both
/// find duplicates the same way.
/// </summary>
public sealed class KnownQuestionTexts
{
    private readonly HashSet<string> _stored;
    private readonly HashSet<string> _taken = new(StringComparer.Ordinal);

    private KnownQuestionTexts(IReadOnlyList<string> stored)
    {
        Stored = stored;
        _stored = stored.Select(QuestionTextNormalizer.Normalize).ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>The category's stored texts as they are, for telling the provider what to avoid.</summary>
    public IReadOnlyList<string> Stored { get; }

    public static async Task<KnownQuestionTexts> LoadAsync(
        IQuestionRepository questions,
        QuestionCategory category,
        CancellationToken cancellationToken)
    {
        // A category that is not saved yet has no questions to collide with.
        IReadOnlyList<string> stored = category.Id == 0
            ? []
            : await questions.GetTextsAsync(category.Id, cancellationToken);

        return new KnownQuestionTexts(stored);
    }

    /// <summary>Takes the text when it is new; otherwise says whom it repeats and leaves it out.</summary>
    /// <returns>Null when taken, <see cref="QuestionRejection.Duplicate"/> or <see cref="QuestionRejection.Repeated"/> when not.</returns>
    public QuestionRejection? TryTake(string textSk)
    {
        var normalized = QuestionTextNormalizer.Normalize(textSk);

        if (_stored.Contains(normalized))
        {
            return QuestionRejection.Duplicate;
        }

        return _taken.Add(normalized) ? null : QuestionRejection.Repeated;
    }
}

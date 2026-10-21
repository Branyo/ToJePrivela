using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.QuestionImport.Dtos;
using ToJePrivela.Application.Questions;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionImport;

/// <summary>
/// Judges imported questions like generated ones: the same <see cref="GeneratedQuestionFilter"/>, and duplicates
/// (<see cref="KnownQuestionTexts"/>) are left out. They are stored as AI questions (<see cref="AiQuestionFactory"/>).
/// </summary>
public sealed class QuestionImporter : IQuestionImporter
{
    /// <summary>Upper bound of items one import may hold.</summary>
    public const int MaxQuestions = 1000;

    /// <summary>Upper bound of the JSON text; generous for <see cref="MaxQuestions"/> questions in two languages.</summary>
    public const int MaxJsonLength = 2_000_000;

    private readonly IQuestionRepository _questions;
    private readonly IBadPointsPicker _badPoints;
    private readonly TimeProvider _timeProvider;

    public QuestionImporter(IQuestionRepository questions, IBadPointsPicker badPoints, TimeProvider timeProvider)
    {
        _questions = questions;
        _badPoints = badPoints;
        _timeProvider = timeProvider;
    }

    public Result<IReadOnlyList<GeneratedQuestion?>> Read(string json)
    {
        var items = QuestionImportParser.Parse(json);

        if (items is null)
        {
            return Result.Failure<IReadOnlyList<GeneratedQuestion?>>(QuestionImportErrors.InvalidJson);
        }

        if (items.Count == 0)
        {
            return Result.Failure<IReadOnlyList<GeneratedQuestion?>>(QuestionImportErrors.Empty);
        }

        return items.Count > MaxQuestions
            ? Result.Failure<IReadOnlyList<GeneratedQuestion?>>(QuestionImportErrors.TooMany(items.Count))
            : Result.Success(items);
    }

    public async Task<QuestionImportOutcome> ToQuestionsAsync(
        QuestionCategory category,
        IReadOnlyList<GeneratedQuestion?> items,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(items);

        var knownTexts = await KnownQuestionTexts.LoadAsync(_questions, category, cancellationToken);
        var createdAt = _timeProvider.GetUtcNow().UtcDateTime;
        var questions = new List<Question>(items.Count);
        var rejected = new List<RejectedQuestionDto>();

        foreach (var (item, index) in items.Select((item, index) => (item, index)))
        {
            var rejection = item is null
                ? QuestionRejection.Malformed
                : GeneratedQuestionFilter.Check(item) ?? knownTexts.TryTake(item.TextSk);

            if (rejection is { } reason)
            {
                rejected.Add(new RejectedQuestionDto(index + 1, reason));
                continue;
            }

            questions.Add(AiQuestionFactory.Create(item!, category, _badPoints, createdAt));
        }

        return new QuestionImportOutcome(items.Count, questions, rejected);
    }
}

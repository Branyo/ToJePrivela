using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Questions;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionGeneration;

/// <summary>
/// Splits a request into calls of <see cref="QuestionGenerationOptions.QuestionsPerRequest"/>, runs them in
/// parallel on distinct subtopics, drops duplicates (against the category and each other) and tops up the
/// shortfall until the requested count is reached or the call budget is spent.
/// </summary>
/// <remarks>
/// The call budget is the number of planned calls plus <see cref="QuestionGenerationOptions.MaxRetryAttempts"/>,
/// so the cost of one request is bounded however badly the provider behaves.
/// </remarks>
public sealed class QuestionGenerationService : IQuestionGenerationService
{
    private readonly IQuestionGenerator _generator;
    private readonly IQuestionRepository _questions;
    private readonly IBadPointsPicker _badPoints;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<QuestionGenerationService> _logger;
    private readonly QuestionGenerationOptions _options;

    public QuestionGenerationService(
        IQuestionGenerator generator,
        IQuestionRepository questions,
        IBadPointsPicker badPoints,
        TimeProvider timeProvider,
        IOptions<QuestionGenerationOptions> options,
        ILogger<QuestionGenerationService> logger)
    {
        _generator = generator;
        _questions = questions;
        _badPoints = badPoints;
        _timeProvider = timeProvider;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<QuestionGenerationResult> GenerateAsync(
        QuestionCategory category,
        int count,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
        {
            return QuestionGenerationResult.Nothing;
        }

        // A category that is not saved yet has no questions to collide with.
        IReadOnlyList<string> existing = category.Id == 0
            ? []
            : await _questions.GetTextsAsync(category.Id, cancellationToken);
        var knownTexts = existing.Select(QuestionTextNormalizer.Normalize).ToHashSet(StringComparer.Ordinal);

        var plannedCalls = (count + _options.QuestionsPerRequest - 1) / _options.QuestionsPerRequest;
        var callBudget = plannedCalls + _options.MaxRetryAttempts;
        IReadOnlyList<string> subtopics = plannedCalls > 1
            ? await _generator.GenerateSubtopicsAsync(category.Name, plannedCalls, cancellationToken)
            : [];

        var accepted = new List<GeneratedQuestion>(count);
        var discarded = 0;
        var callsMade = 0;

        while (accepted.Count < count && callsMade < callBudget)
        {
            var excluded = accepted.Select(q => q.Text).Reverse()
                .Concat(existing)
                .Take(_options.MaxExcludedQuestions)
                .ToList();

            var requests = SplitIntoBatches(count - accepted.Count)
                .Take(callBudget - callsMade)
                .Select((size, index) => new QuestionGenerationRequest(
                    category.Name,
                    size,
                    PickSubtopic(subtopics, callsMade + index),
                    excluded))
                .ToList();

            callsMade += requests.Count;

            foreach (var reply in await CallInParallelAsync(requests, cancellationToken))
            {
                foreach (var question in reply)
                {
                    if (accepted.Count < count && knownTexts.Add(QuestionTextNormalizer.Normalize(question.Text)))
                    {
                        accepted.Add(question);
                    }
                    else
                    {
                        discarded++;
                    }
                }
            }
        }

        LogOutcome(category.Name, count, accepted.Count, discarded, callsMade);

        var createdAt = _timeProvider.GetUtcNow().UtcDateTime;
        var questions = accepted
            .Select(q => new Question(q.Text, q.Answer, category, _badPoints.Pick(), QuestionSource.Ai, createdAt))
            .ToList();

        return new QuestionGenerationResult(questions, count, discarded);
    }

    private IEnumerable<int> SplitIntoBatches(int missing)
    {
        for (var remaining = missing; remaining > 0; remaining -= _options.QuestionsPerRequest)
        {
            yield return Math.Min(remaining, _options.QuestionsPerRequest);
        }
    }

    /// <summary>Retries keep rotating through the subtopics, so top-ups do not all hit the same one.</summary>
    private static string? PickSubtopic(IReadOnlyList<string> subtopics, int callIndex) =>
        subtopics.Count == 0 ? null : subtopics[callIndex % subtopics.Count];

    private async Task<IReadOnlyList<GeneratedQuestion>[]> CallInParallelAsync(
        IReadOnlyList<QuestionGenerationRequest> requests,
        CancellationToken cancellationToken)
    {
        using var throttle = new SemaphoreSlim(_options.MaxParallelRequests);

        return await Task.WhenAll(requests.Select(async request =>
        {
            await throttle.WaitAsync(cancellationToken);

            try
            {
                return await _generator.GenerateAsync(request, cancellationToken);
            }
            finally
            {
                throttle.Release();
            }
        }));
    }

    private void LogOutcome(string category, int requested, int created, int discarded, int calls)
    {
        if (created < requested)
        {
            _logger.LogWarning(
                "Generated only {Created} of {Requested} questions for category {Category} after {Calls} calls ({Discarded} discarded).",
                created,
                requested,
                category,
                calls,
                discarded);
        }
        else
        {
            _logger.LogInformation(
                "Generated {Created} questions for category {Category} in {Calls} calls ({Discarded} discarded).",
                created,
                category,
                calls,
                discarded);
        }
    }
}

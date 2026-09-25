using System.Collections.Concurrent;
using ToJePrivela.Application.Abstractions.Ai;

namespace ToJePrivela.Application.Tests.QuestionGeneration;

/// <summary>Records every call and how many ran at once; replies come from a callback.</summary>
public sealed class FakeQuestionGenerator : IQuestionGenerator
{
    private readonly ConcurrentQueue<QuestionGenerationRequest> _requests = new();
    private int _running;
    private int _maxRunning;
    private int _nextNumber;

    /// <summary>Default: as many fresh, unique questions as were asked for.</summary>
    public Func<QuestionGenerationRequest, IReadOnlyList<GeneratedQuestion>> Reply { get; set; }

    public IReadOnlyList<string> Subtopics { get; set; } = [];

    public TimeSpan Delay { get; set; } = TimeSpan.Zero;

    public int SubtopicCalls { get; private set; }

    public int? RequestedSubtopicCount { get; private set; }

    public IReadOnlyList<QuestionGenerationRequest> Requests => _requests.ToList();

    public int MaxConcurrentCalls => _maxRunning;

    public FakeQuestionGenerator()
    {
        Reply = request => Fresh(request.Count);
    }

    /// <summary>Questions that no other call has returned.</summary>
    public IReadOnlyList<GeneratedQuestion> Fresh(int count) =>
        Enumerable.Range(0, count)
            .Select(_ => Interlocked.Increment(ref _nextNumber))
            .Select(number => new GeneratedQuestion($"Generated question number {number}?", number.ToString()))
            .ToList();

    public Task<IReadOnlyList<string>> GenerateSubtopicsAsync(string category, int count, CancellationToken cancellationToken = default)
    {
        SubtopicCalls++;
        RequestedSubtopicCount = count;
        return Task.FromResult(Subtopics);
    }

    public async Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(
        QuestionGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _requests.Enqueue(request);

        var running = Interlocked.Increment(ref _running);
        InterlockedMax(ref _maxRunning, running);

        try
        {
            if (Delay > TimeSpan.Zero)
            {
                await Task.Delay(Delay, cancellationToken);
            }

            return Reply(request);
        }
        finally
        {
            Interlocked.Decrement(ref _running);
        }
    }

    private static void InterlockedMax(ref int target, int value)
    {
        int current;

        while (value > (current = Volatile.Read(ref target)))
        {
            if (Interlocked.CompareExchange(ref target, value, current) == current)
            {
                return;
            }
        }
    }
}

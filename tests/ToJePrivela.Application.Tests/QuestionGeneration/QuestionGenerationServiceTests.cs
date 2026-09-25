using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.QuestionGeneration;

public class QuestionGenerationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 24, 12, 30, 0, TimeSpan.Zero);

    private readonly QuestionCategory _sport = TestEntities.Category(2, "Sport");
    private readonly FakeQuestionGenerator _generator = new();
    private readonly IQuestionRepository _questions = Substitute.For<IQuestionRepository>();
    private readonly QuestionGenerationOptions _options = new()
    {
        QuestionsPerRequest = 20,
        MaxRetryAttempts = 3,
        MaxParallelRequests = 3,
        MaxExcludedQuestions = 100
    };

    public QuestionGenerationServiceTests()
    {
        _questions.GetTextsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
    }

    private QuestionGenerationService CreateSut() => new(
        _generator,
        _questions,
        new FixedBadPointsPicker(4),
        new FixedTimeProvider(Now),
        Options.Create(_options),
        NullLogger<QuestionGenerationService>.Instance);

    [Fact]
    public async Task GenerateAsync_DoesNothingForZeroQuestions()
    {
        var result = await CreateSut().GenerateAsync(_sport, 0);

        Assert.Empty(result.Questions);
        Assert.False(result.Failed);
        Assert.Empty(_generator.Requests);
        Assert.Equal(0, _generator.SubtopicCalls);
    }

    [Fact]
    public async Task GenerateAsync_BuildsAiQuestionsForTheCategory()
    {
        var result = await CreateSut().GenerateAsync(_sport, 3);

        Assert.Equal(3, result.Created);
        Assert.Equal(3, result.Requested);
        Assert.Equal(0, result.Discarded);
        Assert.All(result.Questions, question =>
        {
            Assert.Same(_sport, question.Category);
            Assert.Equal(QuestionSource.Ai, question.Source);
            Assert.Equal(4, question.BadPoints);
            Assert.Equal(Now.UtcDateTime, question.CreatedAt);
        });
    }

    [Fact]
    public async Task GenerateAsync_UsesOneCallWithoutSubtopicsForASmallRequest()
    {
        await CreateSut().GenerateAsync(_sport, 20);

        var request = Assert.Single(_generator.Requests);
        Assert.Equal("Sport", request.Category);
        Assert.Equal(20, request.Count);
        Assert.Null(request.Subtopic);
        Assert.Equal(0, _generator.SubtopicCalls);
    }

    [Fact]
    public async Task GenerateAsync_SplitsALargeRequestIntoBatchesWithDistinctSubtopics()
    {
        _generator.Subtopics = ["Football", "Tennis", "Hockey"];

        var result = await CreateSut().GenerateAsync(_sport, 45);

        Assert.Equal(45, result.Created);
        Assert.Equal(3, _generator.RequestedSubtopicCount);
        Assert.Equal([5, 20, 20], _generator.Requests.Select(r => r.Count).Order());
        Assert.Equal(["Football", "Hockey", "Tennis"], _generator.Requests.Select(r => r.Subtopic).Order());
    }

    [Fact]
    public async Task GenerateAsync_WorksWithoutSubtopicsWhenNoneComeBack()
    {
        _generator.Subtopics = [];

        var result = await CreateSut().GenerateAsync(_sport, 40);

        Assert.Equal(40, result.Created);
        Assert.All(_generator.Requests, request => Assert.Null(request.Subtopic));
    }

    [Fact]
    public async Task GenerateAsync_NeverRunsMoreCallsAtOnceThanConfigured()
    {
        _options.QuestionsPerRequest = 1;
        _options.MaxParallelRequests = 2;
        _generator.Delay = TimeSpan.FromMilliseconds(20);

        var result = await CreateSut().GenerateAsync(_sport, 8);

        Assert.Equal(8, result.Created);
        Assert.Equal(8, _generator.Requests.Count);
        Assert.Equal(2, _generator.MaxConcurrentCalls);
    }

    [Fact]
    public async Task GenerateAsync_SkipsQuestionsTheCategoryAlreadyHasIgnoringCaseSpacingAndPunctuation()
    {
        _questions.GetTextsAsync(2, Arg.Any<CancellationToken>()).Returns(["How many players are on a football pitch?"]);
        _generator.Reply = request => request.ExcludedQuestions!.Count == 1
            ? [new GeneratedQuestion("  how many PLAYERS are on a   football pitch ", "11"), .. _generator.Fresh(1)]
            : _generator.Fresh(request.Count);

        var result = await CreateSut().GenerateAsync(_sport, 2);

        Assert.Equal(2, result.Created);
        Assert.Equal(1, result.Discarded);
        Assert.DoesNotContain(result.Questions, q => q.Answer == "11");
    }

    [Fact]
    public async Task GenerateAsync_SkipsDuplicatesReturnedByDifferentCalls()
    {
        _options.QuestionsPerRequest = 2;
        var shared = new GeneratedQuestion("Which year was ChatGPT publicly released?", "2022");
        _generator.Reply = request => [shared, .. _generator.Fresh(request.Count)];

        var result = await CreateSut().GenerateAsync(_sport, 4);

        Assert.Equal(4, result.Created);
        Assert.Single(result.Questions, q => q.Answer == "2022");
        Assert.True(result.Discarded >= 1);
    }

    [Fact]
    public async Task GenerateAsync_TopsUpTheShortfallAndTellsTheModelWhatItAlreadyHas()
    {
        _questions.GetTextsAsync(2, Arg.Any<CancellationToken>()).Returns(["An existing question in the category?"]);
        var calls = 0;
        _generator.Reply = request => Interlocked.Increment(ref calls) == 1
            ? _generator.Fresh(request.Count - 3)
            : _generator.Fresh(request.Count);

        var result = await CreateSut().GenerateAsync(_sport, 10);

        Assert.Equal(10, result.Created);
        Assert.Equal(2, _generator.Requests.Count);

        var topUp = _generator.Requests[1];
        Assert.Equal(3, topUp.Count);
        Assert.Equal(8, topUp.ExcludedQuestions!.Count);
        Assert.Equal("An existing question in the category?", topUp.ExcludedQuestions[^1]);
    }

    [Fact]
    public async Task GenerateAsync_CapsTheExcludedQuestions()
    {
        _options.MaxExcludedQuestions = 2;
        _questions.GetTextsAsync(2, Arg.Any<CancellationToken>())
            .Returns(["First existing question?", "Second existing question?", "Third existing question?"]);

        await CreateSut().GenerateAsync(_sport, 1);

        Assert.Equal(
            ["First existing question?", "Second existing question?"],
            Assert.Single(_generator.Requests).ExcludedQuestions!);
    }

    [Fact]
    public async Task GenerateAsync_StopsWhenTheCallBudgetIsSpent()
    {
        _options.MaxRetryAttempts = 2;
        _generator.Reply = _ => [];

        var result = await CreateSut().GenerateAsync(_sport, 45);

        Assert.Empty(result.Questions);
        Assert.True(result.Failed);
        Assert.Equal(3 + 2, _generator.Requests.Count);
    }

    [Fact]
    public async Task GenerateAsync_KeepsWhatItHasWhenTheBudgetRunsOutPartWay()
    {
        _options.MaxRetryAttempts = 1;
        _generator.Reply = request => _generator.Fresh(Math.Min(request.Count, 4));

        var result = await CreateSut().GenerateAsync(_sport, 20);

        Assert.Equal(8, result.Created);
        Assert.False(result.Failed);
        Assert.Equal(2, _generator.Requests.Count);
    }

    [Fact]
    public async Task GenerateAsync_NeverReturnsMoreThanRequested()
    {
        _generator.Reply = request => _generator.Fresh(request.Count + 5);

        var result = await CreateSut().GenerateAsync(_sport, 3);

        Assert.Equal(3, result.Created);
        Assert.Equal(5, result.Discarded);
    }

    [Fact]
    public async Task GenerateAsync_DoesNotLookUpQuestionsOfAnUnsavedCategory()
    {
        await CreateSut().GenerateAsync(new QuestionCategory("Brand new"), 2);

        await _questions.DidNotReceive().GetTextsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_StopsWhenCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => CreateSut().GenerateAsync(_sport, 5, cancellation.Token));
    }
}

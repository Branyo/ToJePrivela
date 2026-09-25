using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ToJePrivela.Ai.OpenAi;
using ToJePrivela.Ai.Parsing;
using ToJePrivela.Ai.Prompts;
using ToJePrivela.Application.Abstractions.Ai;

namespace ToJePrivela.Ai.Tests.OpenAi;

public class OpenAiQuestionGeneratorTests
{
    private const string ValidQuestion = "Which year was ChatGPT publicly released?";

    private readonly IChatCompletionClient _client = Substitute.For<IChatCompletionClient>();
    private readonly IQuestionPromptBuilder _promptBuilder = Substitute.For<IQuestionPromptBuilder>();
    private readonly IGeneratedQuestionParser _parser = Substitute.For<IGeneratedQuestionParser>();
    private readonly OpenAiOptions _options = new() { Language = "Slovak" };

    private OpenAiQuestionGenerator CreateSut() => new(
        _client,
        _promptBuilder,
        _parser,
        Options.Create(_options),
        NullLogger<OpenAiQuestionGenerator>.Instance);

    [Fact]
    public async Task GenerateAsync_MapsParsedQuestions()
    {
        _parser.Parse(Arg.Any<string>()).Returns([new ParsedQuestion($" {ValidQuestion} ", " 2022 ")]);

        var questions = await CreateSut().GenerateAsync(new QuestionGenerationRequest("Sport", 5));

        var question = Assert.Single(questions);
        Assert.Equal(ValidQuestion, question.Text);
        Assert.Equal("2022", question.Answer);
    }

    [Fact]
    public async Task GenerateAsync_BuildsThePromptInTheConfiguredLanguage()
    {
        var request = new QuestionGenerationRequest("Sport", 5, "Football", ["Some existing question?"]);
        _promptBuilder.BuildQuestions(request, "Slovak").Returns("the prompt");
        _parser.Parse(Arg.Any<string>()).Returns([]);

        await CreateSut().GenerateAsync(request);

        await _client.Received(1).CompleteAsync("the prompt", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_DropsQuestionsTheDomainWouldReject()
    {
        _parser.Parse(Arg.Any<string>()).Returns(
        [
            new ParsedQuestion(ValidQuestion, "2022"),
            new ParsedQuestion(ValidQuestion, "two thousand"),
            new ParsedQuestion(ValidQuestion, "3,5"),
            new ParsedQuestion("Short", "5")
        ]);

        var questions = await CreateSut().GenerateAsync(new QuestionGenerationRequest("Sport", 5));

        Assert.Equal("2022", Assert.Single(questions).Answer);
    }

    [Fact]
    public async Task GenerateAsync_NeverReturnsMoreThanRequested()
    {
        _parser.Parse(Arg.Any<string>()).Returns(
        [
            new ParsedQuestion(ValidQuestion, "1"),
            new ParsedQuestion(ValidQuestion, "2"),
            new ParsedQuestion(ValidQuestion, "3")
        ]);

        var questions = await CreateSut().GenerateAsync(new QuestionGenerationRequest("Sport", 2));

        Assert.Equal(2, questions.Count);
    }

    [Fact]
    public async Task GenerateAsync_MakesExactlyOneCallEvenWhenNothingIsUsable()
    {
        _parser.Parse(Arg.Any<string>()).Returns([]);

        var questions = await CreateSut().GenerateAsync(new QuestionGenerationRequest("Sport", 5));

        Assert.Empty(questions);
        await _client.Received(1).CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_StopsWhenCancelled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => CreateSut().GenerateAsync(new QuestionGenerationRequest("Sport", 5), cancellation.Token));

        await _client.DidNotReceive().CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateSubtopicsAsync_ReturnsTheParsedSubtopics()
    {
        _promptBuilder.BuildSubtopics("Sport", 3, "Slovak").Returns("subtopic prompt");
        _client.CompleteAsync("subtopic prompt", Arg.Any<CancellationToken>()).Returns("reply");
        _parser.ParseSubtopics("reply").Returns(["Football", "Tennis", "Hockey"]);

        var subtopics = await CreateSut().GenerateSubtopicsAsync("Sport", 3);

        Assert.Equal(["Football", "Tennis", "Hockey"], subtopics);
    }

    [Fact]
    public async Task GenerateSubtopicsAsync_NeverReturnsMoreThanRequested()
    {
        _parser.ParseSubtopics(Arg.Any<string>()).Returns(["Football", "Tennis", "Hockey"]);

        var subtopics = await CreateSut().GenerateSubtopicsAsync("Sport", 2);

        Assert.Equal(["Football", "Tennis"], subtopics);
    }

    [Fact]
    public async Task GenerateSubtopicsAsync_ReturnsNothingWhenTheCallFails()
    {
        _client.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        _parser.ParseSubtopics(null).Returns([]);

        Assert.Empty(await CreateSut().GenerateSubtopicsAsync("Sport", 3));
    }
}

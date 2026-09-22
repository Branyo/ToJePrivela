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
    private readonly OpenAiOptions _options = new() { MaxRetryAttempts = 3, DefaultCategory = "Arbitrary", DefaultLanguage = "Slovak", DefaultDifficulty = 3 };

    private OpenAiQuestionGenerator CreateSut() => new(
        _client,
        _promptBuilder,
        _parser,
        Options.Create(_options),
        NullLogger<OpenAiQuestionGenerator>.Instance);

    [Fact]
    public async Task GenerateAsync_MapsParsedQuestions()
    {
        _parser.Parse(Arg.Any<string>()).Returns([new ParsedQuestion(ValidQuestion, "2022")]);

        var questions = await CreateSut().GenerateAsync(new QuestionGenerationRequest("Sport", 5));

        var question = Assert.Single(questions);
        Assert.Equal(ValidQuestion, question.Text);
        Assert.Equal("2022", question.Answer);
        Assert.Equal("Sport", question.Category);
        Assert.Equal(3, question.Difficulty);
    }

    [Fact]
    public async Task GenerateAsync_UsesConfiguredDefaultsWhenNothingIsRequested()
    {
        _parser.Parse(Arg.Any<string>()).Returns([new ParsedQuestion(ValidQuestion, "2022")]);

        var questions = await CreateSut().GenerateAsync(new QuestionGenerationRequest(null, 5));

        Assert.Equal("Arbitrary", Assert.Single(questions).Category);
        _promptBuilder.Received(1).Build("Arbitrary", 5, "Slovak");
    }

    [Fact]
    public async Task GenerateAsync_UsesTheRequestedDifficulty()
    {
        _parser.Parse(Arg.Any<string>()).Returns([new ParsedQuestion(ValidQuestion, "2022")]);

        var questions = await CreateSut().GenerateAsync(new QuestionGenerationRequest("Sport", 5, "English", 5));

        Assert.Equal(5, Assert.Single(questions).Difficulty);
        _promptBuilder.Received(1).Build("Sport", 5, "English");
    }

    [Fact]
    public async Task GenerateAsync_DropsQuestionsTheDomainWouldReject()
    {
        _parser.Parse(Arg.Any<string>()).Returns(
        [
            new ParsedQuestion(ValidQuestion, "2022"),
            new ParsedQuestion(ValidQuestion, "two thousand"),
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
    public async Task GenerateAsync_RetriesUntilTheModelReturnsSomethingUsable()
    {
        _parser.Parse(Arg.Any<string>()).Returns(
            _ => [],
            _ => [new ParsedQuestion(ValidQuestion, "2022")]);

        var questions = await CreateSut().GenerateAsync(new QuestionGenerationRequest("Sport", 5));

        Assert.Single(questions);
        await _client.Received(2).CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAsync_GivesUpAfterTheConfiguredNumberOfAttempts()
    {
        _options.MaxRetryAttempts = 2;
        _parser.Parse(Arg.Any<string>()).Returns([]);

        var questions = await CreateSut().GenerateAsync(new QuestionGenerationRequest("Sport", 5));

        Assert.Empty(questions);
        await _client.Received(2).CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
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
}

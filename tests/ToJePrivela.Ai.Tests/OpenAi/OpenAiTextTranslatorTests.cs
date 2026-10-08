using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ToJePrivela.Ai.OpenAi;
using ToJePrivela.Ai.Parsing;
using ToJePrivela.Ai.Prompts;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Ai.Tests.OpenAi;

public class OpenAiTextTranslatorTests
{
    private readonly IChatCompletionClient _client = Substitute.For<IChatCompletionClient>();

    private OpenAiTextTranslator CreateSut() => new(_client, new TranslationPromptBuilder(), new TranslationParser());

    [Fact]
    public async Task TranslateAsync_MakesOneCallAndReturnsTheParsedTranslation()
    {
        _client.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("\"Birds\"");

        var translation = await CreateSut().TranslateAsync("Vtáky", Language.Sk, Language.En);

        Assert.Equal("Birds", translation);
        await _client.Received(1).CompleteAsync(
            Arg.Is<string>(prompt => prompt.Contains("from Slovak to English: Vtáky")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TranslateAsync_ReturnsNullWithoutAReadableReply()
    {
        _client.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        Assert.Null(await CreateSut().TranslateAsync("Vtáky", Language.Sk, Language.En));
    }

    [Fact]
    public async Task TranslateAsync_SkipsTheCallWithinOneLanguage()
    {
        Assert.Equal("Vtáky", await CreateSut().TranslateAsync(" Vtáky ", Language.Sk, Language.Sk));
        await _client.DidNotReceiveWithAnyArgs().CompleteAsync(default!, default);
    }

    [Fact]
    public async Task TranslateAsync_LetsAnUnavailableProviderSurface()
    {
        _client.CompleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new QuestionGeneratorUnavailableException("down"));

        await Assert.ThrowsAsync<QuestionGeneratorUnavailableException>(
            () => CreateSut().TranslateAsync("Vtáky", Language.Sk, Language.En));
    }
}

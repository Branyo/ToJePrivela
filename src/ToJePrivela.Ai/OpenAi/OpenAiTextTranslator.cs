using ToJePrivela.Ai.Parsing;
using ToJePrivela.Ai.Prompts;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Ai.OpenAi;

/// <summary>One provider call per translation; whether the result is usable is the Application layer's call.</summary>
public sealed class OpenAiTextTranslator : ITextTranslator
{
    private readonly IChatCompletionClient _client;
    private readonly ITranslationPromptBuilder _promptBuilder;
    private readonly ITranslationParser _parser;

    public OpenAiTextTranslator(
        IChatCompletionClient client,
        ITranslationPromptBuilder promptBuilder,
        ITranslationParser parser)
    {
        _client = client;
        _promptBuilder = promptBuilder;
        _parser = parser;
    }

    public async Task<string?> TranslateAsync(string text, Language from, Language to, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        cancellationToken.ThrowIfCancellationRequested();

        if (from == to)
        {
            return text.Trim();
        }

        var reply = await _client.CompleteAsync(_promptBuilder.Build(text, from, to), cancellationToken);
        return _parser.Parse(reply);
    }
}

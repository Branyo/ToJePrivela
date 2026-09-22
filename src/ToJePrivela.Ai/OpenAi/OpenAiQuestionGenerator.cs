using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToJePrivela.Ai.Parsing;
using ToJePrivela.Ai.Prompts;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Ai.OpenAi;

public sealed class OpenAiQuestionGenerator : IQuestionGenerator
{
    private readonly IChatCompletionClient _client;
    private readonly IQuestionPromptBuilder _promptBuilder;
    private readonly IGeneratedQuestionParser _parser;
    private readonly ILogger<OpenAiQuestionGenerator> _logger;
    private readonly OpenAiOptions _options;

    public OpenAiQuestionGenerator(
        IChatCompletionClient client,
        IQuestionPromptBuilder promptBuilder,
        IGeneratedQuestionParser parser,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiQuestionGenerator> logger)
    {
        _client = client;
        _promptBuilder = promptBuilder;
        _parser = parser;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(
        QuestionGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var category = Fallback(request.Category, _options.DefaultCategory);
        var language = Fallback(request.Language, _options.DefaultLanguage);
        var difficulty = request.Difficulty ?? _options.DefaultDifficulty;
        var prompt = _promptBuilder.Build(category, request.Count, language);

        for (var attempt = 1; attempt <= _options.MaxRetryAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var reply = await _client.CompleteAsync(prompt, cancellationToken);
            var questions = _parser.Parse(reply)
                .Select(parsed => ToGeneratedQuestion(parsed, category, difficulty))
                .OfType<GeneratedQuestion>()
                .Take(request.Count)
                .ToList();

            if (questions.Count > 0)
            {
                return questions;
            }

            _logger.LogWarning(
                "Attempt {Attempt}/{MaxAttempts} returned no usable questions for category {Category}.",
                attempt,
                _options.MaxRetryAttempts,
                category);
        }

        return [];
    }

    /// <summary>Drops items the domain would reject, so the caller never sees an unusable question.</summary>
    private static GeneratedQuestion? ToGeneratedQuestion(ParsedQuestion parsed, string category, int difficulty)
    {
        var text = parsed.Question.Trim();
        var answer = parsed.Answer.Trim();

        if (text.Length < Question.TextMinLength || text.Length > Question.TextMaxLength)
        {
            return null;
        }

        return Guard.IsNumeric(answer)
            ? new GeneratedQuestion(text, answer, category, difficulty)
            : null;
    }

    private static string Fallback(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}

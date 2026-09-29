using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToJePrivela.Ai.Parsing;
using ToJePrivela.Ai.Prompts;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Ai.OpenAi;

/// <summary>One provider call per method; retries and batching are the Application layer's job.</summary>
public sealed partial class OpenAiQuestionGenerator : IQuestionGenerator
{
    /// <summary>Anything larger is unreadable in a quiz; the prompt asks for "how many millions/billions" instead.</summary>
    public const decimal MaxAnswer = 1_000_000_000_000m;

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

    public async Task<IReadOnlyList<string>> GenerateSubtopicsAsync(
        string category,
        int count,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var prompt = _promptBuilder.BuildSubtopics(category, count, _options.Language);
        var subtopics = _parser.ParseSubtopics(await _client.CompleteAsync(prompt, cancellationToken))
            .Take(count)
            .ToList();

        if (subtopics.Count == 0)
        {
            _logger.LogWarning("No subtopics came back for category {Category}.", category);
        }

        return subtopics;
    }

    public async Task<IReadOnlyList<GeneratedQuestion>> GenerateAsync(
        QuestionGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var prompt = _promptBuilder.BuildQuestions(request, _options.Language);
        var reply = await _client.CompleteAsync(prompt, cancellationToken);

        return _parser.Parse(reply)
            .Select(ToGeneratedQuestion)
            .OfType<GeneratedQuestion>()
            .Take(request.Count)
            .ToList();
    }

    /// <summary>
    /// Drops items the domain would reject, answers above <see cref="MaxAnswer"/> and questions that
    /// state their own answer, so the caller never sees an unusable question.
    /// </summary>
    private static GeneratedQuestion? ToGeneratedQuestion(ParsedQuestion parsed)
    {
        var text = parsed.Question.Trim();
        var answer = parsed.Answer.Trim();

        if (text.Length < Question.TextMinLength || text.Length > Question.TextMaxLength)
        {
            return null;
        }

        if (!Guard.IsNumeric(answer))
        {
            return null;
        }

        var value = Math.Abs(decimal.Parse(answer, NumberStyles.Float, CultureInfo.InvariantCulture));

        return value > MaxAnswer || StatesNumber(text, value)
            ? null
            : new GeneratedQuestion(text, answer);
    }

    /// <summary>A question that states its own answer gives it away ("In 1969, which year did Apollo 11 land?").</summary>
    private static bool StatesNumber(string text, decimal value) =>
        NumberInText().Matches(text).Any(match => decimal.TryParse(
            string.Concat(match.Value.Where(character => !char.IsWhiteSpace(character))).Replace(',', '.'),
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var number) && number == value);

    /// <summary>Plain numbers, decimals with either separator and space-grouped thousands ("1 000 000").</summary>
    [GeneratedRegex(@"\d{1,3}(?:\s\d{3})+|\d+(?:[.,]\d+)?")]
    private static partial Regex NumberInText();
}

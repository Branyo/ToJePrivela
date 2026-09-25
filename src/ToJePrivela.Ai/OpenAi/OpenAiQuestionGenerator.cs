using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToJePrivela.Ai.Parsing;
using ToJePrivela.Ai.Prompts;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Ai.OpenAi;

/// <summary>One provider call per method; retries and batching are the Application layer's job.</summary>
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

    /// <summary>Drops items the domain would reject, so the caller never sees an unusable question.</summary>
    private static GeneratedQuestion? ToGeneratedQuestion(ParsedQuestion parsed)
    {
        var text = parsed.Question.Trim();
        var answer = parsed.Answer.Trim();

        if (text.Length < Question.TextMinLength || text.Length > Question.TextMaxLength)
        {
            return null;
        }

        return Guard.IsNumeric(answer)
            ? new GeneratedQuestion(text, answer)
            : null;
    }
}

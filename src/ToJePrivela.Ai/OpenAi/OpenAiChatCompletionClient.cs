using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToJePrivela.Ai.OpenAi.Contracts;
using ToJePrivela.Application.Abstractions.Ai;

namespace ToJePrivela.Ai.OpenAi;

public sealed class OpenAiChatCompletionClient : IChatCompletionClient
{
    private const string UserRole = "user";
    private const string NoReasoning = "none";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenAiChatCompletionClient> _logger;
    private readonly OpenAiOptions _options;

    public OpenAiChatCompletionClient(
        HttpClient httpClient,
        IOptions<OpenAiOptions> options,
        ILogger<OpenAiChatCompletionClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<string?> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new QuestionGeneratorUnavailableException(
                "OpenAi:ApiKey is not configured; set it through user-secrets or the OpenAi__ApiKey environment variable.");
        }

        var payload = new ChatCompletionRequest(
            _options.Model,
            [new ChatMessage(UserRole, prompt)],
            _options.MaxTokens,
            _options.ReasoningEffort,
            // Reasoning requests reject temperature with a 400.
            _options.ReasoningEffort == NoReasoning ? _options.Temperature : null);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Url)
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            // Wrong key, exhausted quota, bad model name or an outage: none of them clears up on a retry.
            if (!response.IsSuccessStatusCode)
            {
                throw new QuestionGeneratorUnavailableException(
                    $"Chat completion request failed with status {(int)response.StatusCode}.");
            }

            var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
                SerializerOptions,
                cancellationToken);

            return completion?.Choices?.FirstOrDefault()?.Message?.Content;
        }
        catch (JsonException exception)
        {
            // The provider answered, just not readably: that reply is unusable, the provider is not down.
            _logger.LogWarning(exception, "Chat completion reply could not be read.");
            return null;
        }
        // A timeout surfaces as TaskCanceledException too; only the caller's own cancellation propagates.
        catch (Exception exception) when (
            exception is HttpRequestException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            throw new QuestionGeneratorUnavailableException("Chat completion request could not be completed.", exception);
        }
    }
}

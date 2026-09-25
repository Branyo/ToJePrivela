using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToJePrivela.Ai.OpenAi.Contracts;

namespace ToJePrivela.Ai.OpenAi;

public sealed class OpenAiChatCompletionClient : IChatCompletionClient
{
    private const string UserRole = "user";

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
        var payload = new ChatCompletionRequest(
            _options.Model,
            [new ChatMessage(UserRole, prompt)],
            _options.MaxTokens,
            _options.Temperature);

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.Url)
        {
            Content = JsonContent.Create(payload, options: SerializerOptions)
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Chat completion request failed with status {StatusCode}.",
                    (int)response.StatusCode);

                return null;
            }

            var completion = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(
                SerializerOptions,
                cancellationToken);

            return completion?.Choices?.FirstOrDefault()?.Message?.Content;
        }
        // A timeout surfaces as TaskCanceledException too; only the caller's own cancellation propagates.
        catch (Exception exception) when (
            (exception is HttpRequestException or TaskCanceledException or JsonException)
            && !cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Chat completion request could not be completed.");
            return null;
        }
    }
}

using System.Text.Json.Serialization;

namespace ToJePrivela.Ai.OpenAi.Contracts;

internal sealed record ChatCompletionRequest(
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
    [property: JsonPropertyName("max_completion_tokens")] int MaxCompletionTokens,
    [property: JsonPropertyName("reasoning_effort")] string ReasoningEffort,
    [property: JsonPropertyName("temperature"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    decimal? Temperature);

internal sealed record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

internal sealed record ChatCompletionResponse(
    [property: JsonPropertyName("choices")] IReadOnlyList<ChatChoice>? Choices);

internal sealed record ChatChoice(
    [property: JsonPropertyName("message")] ChatMessage? Message);

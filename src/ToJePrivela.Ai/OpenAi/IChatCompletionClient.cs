namespace ToJePrivela.Ai.OpenAi;

/// <summary>Transport-level access to the chat completion endpoint.</summary>
public interface IChatCompletionClient
{
    /// <summary>Returns the assistant reply, or null when the provider answered without a readable one.</summary>
    /// <exception cref="Application.Abstractions.Ai.QuestionGeneratorUnavailableException">
    /// The provider is not configured, unreachable, timed out or refused the request.
    /// </exception>
    Task<string?> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}

namespace ToJePrivela.Ai.OpenAi;

/// <summary>Transport-level access to the chat completion endpoint.</summary>
public interface IChatCompletionClient
{
    /// <summary>Returns the assistant reply, or null when the call did not succeed.</summary>
    Task<string?> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}

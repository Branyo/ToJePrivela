namespace ToJePrivela.Application.Abstractions.Ai;

/// <summary>
/// Thrown by <see cref="IQuestionGenerator"/> when the provider cannot be used at all (not configured,
/// unreachable, refusing the key or the quota), as opposed to answering with nothing usable. Retrying
/// right away would not help, so the caller stops instead of spending its call budget.
/// </summary>
public sealed class QuestionGeneratorUnavailableException : Exception
{
    public QuestionGeneratorUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

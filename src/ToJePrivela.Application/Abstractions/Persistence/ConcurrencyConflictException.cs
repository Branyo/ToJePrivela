namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>
/// Thrown by <see cref="IUnitOfWork"/> when a row changed or disappeared since it was read, so saving
/// would have overwritten someone else's change.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

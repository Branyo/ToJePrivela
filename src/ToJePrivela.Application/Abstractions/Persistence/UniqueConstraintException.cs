namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>
/// Thrown by <see cref="IUnitOfWork"/> when the database rejects a duplicate that slipped past the
/// use case's own check, typically two concurrent requests creating the same name.
/// </summary>
public sealed class UniqueConstraintException : Exception
{
    public UniqueConstraintException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

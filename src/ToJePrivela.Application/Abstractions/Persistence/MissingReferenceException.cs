namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>
/// Thrown by <see cref="IUnitOfWork"/> when the changes point at a row another request deleted after it was read,
/// typically a write of a login an admin deleted meanwhile.
/// </summary>
public sealed class MissingReferenceException : Exception
{
    public MissingReferenceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

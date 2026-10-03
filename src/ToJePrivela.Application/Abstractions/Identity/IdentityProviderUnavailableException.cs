namespace ToJePrivela.Application.Abstractions.Identity;

/// <summary>
/// Thrown by <see cref="IExternalIdentityVerifier"/> when the provider cannot be asked (unreachable, timing out,
/// answering with an error), as opposed to rejecting the token.
/// </summary>
public sealed class IdentityProviderUnavailableException : Exception
{
    public IdentityProviderUnavailableException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

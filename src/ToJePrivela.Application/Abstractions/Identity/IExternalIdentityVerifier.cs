using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Identity;

/// <summary>Checks a token the browser got from one provider's sign-in; one implementation per provider.</summary>
public interface IExternalIdentityVerifier
{
    IdentityProvider Provider { get; }

    /// <summary>The public id the browser's sign-in SDK needs; null when the provider is not configured.</summary>
    string? ClientId { get; }

    /// <returns>The identity, or null when the token is not valid for this application.</returns>
    /// <exception cref="IdentityProviderUnavailableException">The provider could not be asked.</exception>
    Task<ExternalIdentity?> VerifyAsync(string token, CancellationToken cancellationToken = default);
}

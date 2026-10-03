using Google.Apis.Auth;

namespace ToJePrivela.Identity.Google;

/// <summary>Checks a Google ID token's signature, issuer, expiry and audience; a seam so tests need no network.</summary>
public interface IGoogleIdTokenValidator
{
    /// <exception cref="InvalidJwtException">The token is not a valid ID token for <paramref name="clientId"/>.</exception>
    Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, string clientId);
}

/// <summary>Google's own validation, which fetches (and caches) Google's signing keys.</summary>
public sealed class GoogleJsonWebSignatureValidator : IGoogleIdTokenValidator
{
    public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken, string clientId) =>
        GoogleJsonWebSignature.ValidateAsync(
            idToken,
            new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] });
}

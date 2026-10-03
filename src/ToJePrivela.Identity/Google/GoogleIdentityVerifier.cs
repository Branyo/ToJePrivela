using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Identity.Google;

/// <summary>Verifies the ID token (the <c>credential</c>) that Google Identity Services hands the browser.</summary>
public sealed class GoogleIdentityVerifier : IExternalIdentityVerifier
{
    private readonly IGoogleIdTokenValidator _validator;
    private readonly ILogger<GoogleIdentityVerifier> _logger;

    public GoogleIdentityVerifier(
        IGoogleIdTokenValidator validator,
        IOptions<GoogleOptions> options,
        ILogger<GoogleIdentityVerifier> logger)
    {
        _validator = validator;
        _logger = logger;
        ClientId = string.IsNullOrWhiteSpace(options.Value.ClientId) ? null : options.Value.ClientId.Trim();
    }

    public IdentityProvider Provider => IdentityProvider.Google;

    public string? ClientId { get; }

    public async Task<ExternalIdentity?> VerifyAsync(string token, CancellationToken cancellationToken = default)
    {
        if (ClientId is null || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        GoogleJsonWebSignature.Payload payload;

        try
        {
            payload = await _validator.ValidateAsync(token, ClientId);
        }
        catch (InvalidJwtException exception)
        {
            _logger.LogInformation("Google rejected an ID token: {Reason}", exception.Message);
            return null;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
                                          && !cancellationToken.IsCancellationRequested)
        {
            throw new IdentityProviderUnavailableException("Google's signing keys could not be fetched.", exception);
        }

        if (string.IsNullOrWhiteSpace(payload.Subject))
        {
            return null;
        }

        // An unverified address must never match an admin provisioned by email.
        var email = payload.EmailVerified ? payload.Email : null;

        return new ExternalIdentity(IdentityProvider.Google, payload.Subject, email, payload.Name ?? email ?? string.Empty);
    }
}

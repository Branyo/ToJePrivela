using ToJePrivela.Application.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Accounts;

public static class AccountErrors
{
    /// <summary>No valid access token came with a request that needs one.</summary>
    public static readonly Error Unauthenticated =
        Error.Unauthorized("Auth.Unauthenticated", "Sign in to continue.");

    /// <summary>The signed-in account may not do this (an admin-only action).</summary>
    public static readonly Error Forbidden =
        Error.Forbidden("Auth.Forbidden", "Only an admin may do this.");

    /// <summary>The token names an account that is not stored (e.g. a database that was reset).</summary>
    public static readonly Error UnknownAccount =
        Error.Unauthorized("Auth.UnknownAccount", "The signed-in account no longer exists; sign in again.");

    public static Error ProviderNotConfigured(IdentityProvider provider) =>
        Error.Validation("Auth.ProviderNotConfigured", $"Signing in with {provider} is not configured.");

    public static Error InvalidToken(IdentityProvider provider) =>
        Error.Unauthorized("Auth.InvalidToken", $"{provider} did not confirm the sign-in.");

    public static Error ProviderUnavailable(IdentityProvider provider) =>
        Error.Unavailable("Auth.ProviderUnavailable", $"{provider} could not be reached to confirm the sign-in.");
}

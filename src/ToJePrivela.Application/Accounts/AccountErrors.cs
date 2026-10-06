using ToJePrivela.Application.Common;

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
        Error.Unauthorized("Auth.UnknownAccount", "The signed-in login no longer exists; sign in again.");

    public static readonly Error WrongPassword =
        Error.Unauthorized("Auth.WrongPassword", "The password is not right.");

    /// <summary>
    /// No login has this name. Saying so is deliberate: the client offers to create the login, so whether a name
    /// exists is not a secret, and sign-in attempts are rate limited instead.
    /// </summary>
    public static Error UnknownLogin(string name) =>
        Error.NotFound("Auth.UnknownLogin", $"There is no login named '{name}'.");

    public static Error NameTaken(string name) =>
        Error.Conflict("Auth.NameTaken", $"Login with name '{name}' already exists.");
}

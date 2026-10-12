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

    /// <summary>
    /// The token was issued before the login's password or admin rights changed hands (see
    /// <see cref="Domain.Entities.Account.SecurityStamp"/>), so it no longer counts.
    /// </summary>
    public static readonly Error SignedOut =
        Error.Unauthorized("Auth.SignedOut", "This sign-in has ended because the login changed; sign in again.");

    public static readonly Error WrongPassword =
        Error.Unauthorized("Auth.WrongPassword", "The password is not right.");

    /// <summary>
    /// Changing the password with a wrong current one. A 400, not a 401: the sign-in itself is still fine.
    /// </summary>
    public static readonly Error CurrentPasswordWrong =
        Error.Validation("Auth.CurrentPasswordWrong", "The current password is not right.");

    public static readonly Error SamePassword =
        Error.Validation("Auth.SamePassword", "The new password must differ from the current one.");

    /// <summary>
    /// An admin's password is set in <c>Authentication:Admins</c> and provisioning resets it on every startup, so a
    /// change made here would silently be undone.
    /// </summary>
    public static readonly Error AdminPasswordFromConfig =
        Error.Conflict("Auth.AdminPasswordFromConfig", "An admin's password is set in the server configuration.");

    /// <summary>
    /// No login has this name. Saying so is deliberate: the client offers to create the login, so whether a name
    /// exists is not a secret, and sign-in attempts are rate limited instead.
    /// </summary>
    public static Error UnknownLogin(string name) =>
        Error.NotFound("Auth.UnknownLogin", $"There is no login named '{name}'.");

    public static Error NameTaken(string name) =>
        Error.Conflict("Auth.NameTaken", $"Login with name '{name}' already exists.");
}

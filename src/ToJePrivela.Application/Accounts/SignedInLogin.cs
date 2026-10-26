using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.Accounts;

/// <summary>
/// A change of the signed-in login's own rows that finds one of them gone may have lost a race with an admin deleting
/// the whole login. Then the answer is the same 401 its next request would get, so the client signs out at once,
/// rather than a 404 of a row it never saw deleted.
/// </summary>
public static class SignedInLogin
{
    /// <summary>
    /// <see cref="AccountErrors.UnknownAccount"/> when the login is gone, otherwise <paramref name="notFound"/>; asks
    /// the database only on this path.
    /// </summary>
    public static async Task<Error> GoneOrAsync(
        IAccountRepository accounts,
        int accountId,
        Error notFound,
        CancellationToken cancellationToken) =>
        await accounts.LoginExistsAsync(accountId, cancellationToken) ? notFound : AccountErrors.UnknownAccount;
}

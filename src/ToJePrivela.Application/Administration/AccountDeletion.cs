using ToJePrivela.Application.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Administration;

/// <summary>
/// Which logins an admin may delete: the one rule behind both <c>DELETE /api/admin/accounts/{id}</c> and the
/// <c>canDelete</c> the list of logins shows, so the two never disagree.
/// </summary>
public static class AccountDeletion
{
    /// <summary>Why the admin may not delete the login, or null when they may.</summary>
    public static Error? Refusal(int accountId, int adminId) =>
        accountId == adminId ? AdministrationErrors.CannotDeleteOwnAccount
        : accountId == Account.ReservedId ? AdministrationErrors.CannotDeleteReservedAccount
        : null;
}

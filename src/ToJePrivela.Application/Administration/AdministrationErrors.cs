using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.Administration;

public static class AdministrationErrors
{
    /// <summary>No login has the id (the reserved account is none); it may have been deleted meanwhile.</summary>
    public static Error AccountNotFound(int id) =>
        Error.NotFound("Account.NotFound", $"Login with id {id} was not found.");

    /// <summary>Deleting the login an admin is signed in to would end the sign-in with everything it owns.</summary>
    public static readonly Error CannotDeleteOwnAccount =
        Error.Conflict("Account.CannotDeleteOwn", "You cannot delete the login you are signed in to.");
}

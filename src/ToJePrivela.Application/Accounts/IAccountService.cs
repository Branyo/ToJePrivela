using ToJePrivela.Application.Accounts.Dtos;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.Accounts;

public interface IAccountService
{
    /// <summary>Checks the password and returns the access token for the following requests.</summary>
    Task<Result<SignedInDto>> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default);

    /// <summary>Creates a login (never an admin) and signs it in.</summary>
    Task<Result<SignedInDto>> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken = default);

    Task<Result<AccountDto>> GetCurrentAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The account an access token names, as long as the token still counts for it. The host reads it on every request,
    /// so a revoked admin flag counts at once rather than when the token expires.
    /// <see cref="AccountErrors.UnknownAccount"/> when the login is gone, <see cref="AccountErrors.SignedOut"/> when
    /// the token carries an older <see cref="Domain.Entities.Account.SecurityStamp"/>.
    /// </summary>
    Task<Result<AccountDto>> GetSignedInAsync(int id, string securityStamp, CancellationToken cancellationToken = default);
}

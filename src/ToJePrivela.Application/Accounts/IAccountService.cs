using ToJePrivela.Application.Accounts.Dtos;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.Accounts;

public interface IAccountService
{
    /// <summary>The providers a client can sign in with, i.e. the configured ones.</summary>
    Result<IReadOnlyList<SignInProviderDto>> GetProviders();

    /// <summary>
    /// Verifies the provider's token and signs the identity in: its bound account, else an admin account provisioned
    /// for its email, else a new account. Returns the access token for the following requests.
    /// </summary>
    Task<Result<SignedInDto>> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default);

    Task<Result<AccountDto>> GetCurrentAsync(CancellationToken cancellationToken = default);
}

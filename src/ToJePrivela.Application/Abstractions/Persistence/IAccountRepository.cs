using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IAccountRepository : IRepository<Account>
{
    /// <summary>The account bound to the identity.</summary>
    Task<Account?> GetBoundAsync(IdentityProvider provider, string externalId, CancellationToken cancellationToken = default);

    /// <summary>A provisioned (not yet bound) account waiting for the first sign-in with this email.</summary>
    /// <param name="email">In <see cref="Account.NormalizeEmail"/> form.</param>
    Task<Account?> GetProvisionedAsync(IdentityProvider provider, string email, CancellationToken cancellationToken = default);

    /// <summary>Any account with this email at the provider, bound or not.</summary>
    /// <param name="email">In <see cref="Account.NormalizeEmail"/> form.</param>
    Task<Account?> GetByEmailAsync(IdentityProvider provider, string email, CancellationToken cancellationToken = default);

    /// <summary>The account with <see cref="Account.ReservedId"/> while it is still <see cref="Account.IsReserved"/>.</summary>
    Task<Account?> GetReservedAsync(CancellationToken cancellationToken = default);
}

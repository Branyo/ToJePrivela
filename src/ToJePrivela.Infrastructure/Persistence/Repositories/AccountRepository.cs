using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    public async Task<Account?> GetBoundAsync(IdentityProvider provider, string externalId, CancellationToken cancellationToken = default) =>
        await Set.FirstOrDefaultAsync(a => a.Provider == provider && a.ExternalId == externalId, cancellationToken);

    public async Task<Account?> GetProvisionedAsync(IdentityProvider provider, string email, CancellationToken cancellationToken = default) =>
        await Set
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync(a => a.Provider == provider && a.ExternalId == null && a.Email == email, cancellationToken);

    public async Task<Account?> GetByEmailAsync(IdentityProvider provider, string email, CancellationToken cancellationToken = default) =>
        await Set
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync(a => a.Provider == provider && a.Email == email, cancellationToken);

    public async Task<Account?> GetReservedAsync(CancellationToken cancellationToken = default) =>
        await Set.FirstOrDefaultAsync(
            a => a.Id == Account.ReservedId && a.ExternalId == null && a.Email == null,
            cancellationToken);
}

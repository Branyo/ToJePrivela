using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public sealed class AccountRepository : Repository<Account>, IAccountRepository
{
    public AccountRepository(ToJePrivelaDbContext context) : base(context)
    {
    }

    public async Task<Account?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var key = NameKeys.Of(name);

        // The reserved account's empty key is never a name's.
        return key.Length == 0 ? null : await Set.FirstOrDefaultAsync(a => a.NameKey == key, cancellationToken);
    }

    public async Task<Account?> GetReservedAsync(CancellationToken cancellationToken = default) =>
        await Set.FirstOrDefaultAsync(a => a.Id == Account.ReservedId && a.PasswordHash == null, cancellationToken);

    public async Task<IReadOnlyList<Account>> GetAdminsAsync(CancellationToken cancellationToken = default) =>
        await Set.Where(a => a.IsAdmin).ToListAsync(cancellationToken);
}

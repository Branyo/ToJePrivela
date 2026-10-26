using System.Linq.Expressions;
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

    /// <summary>Newest first puts the logins never signed in last: SQLite sorts NULL lowest.</summary>
    public async Task<IReadOnlyList<LoginSummary>> GetLoginsAsync(CancellationToken cancellationToken = default) =>
        await Logins()
            .OrderByDescending(a => a.LastSignedInAt)
            .ThenByDescending(a => a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .Select(ToSummary)
            .ToListAsync(cancellationToken);

    public async Task<LoginSummary?> GetLoginAsync(int id, CancellationToken cancellationToken = default) =>
        await Logins().Where(a => a.Id == id).Select(ToSummary).FirstOrDefaultAsync(cancellationToken);

    public async Task<bool> LoginExistsAsync(int id, CancellationToken cancellationToken = default) =>
        await Logins().AnyAsync(a => a.Id == id, cancellationToken);

    /// <summary>
    /// The owned rows are deleted first, since their keys to the account restrict its deletion (a login is never
    /// deleted by accident); a game's seats go with it (cascade). The transaction takes SQLite's write lock before the
    /// check, so nothing can be added to the login in between, and a request of the deleted login that saves afterwards
    /// finds its rows gone.
    /// </summary>
    public async Task<bool> DeleteLoginAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);

        if (!await LoginExistsAsync(id, cancellationToken))
        {
            return false;
        }

        await Context.Set<Game>().Where(g => g.AccountId == id).ExecuteDeleteAsync(cancellationToken);
        await Context.Set<Player>().Where(p => p.AccountId == id).ExecuteDeleteAsync(cancellationToken);
        await Context.Set<QuestionView>().Where(v => v.AccountId == id).ExecuteDeleteAsync(cancellationToken);
        await Set.Where(a => a.Id == id).ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>The accounts somebody can sign in to: all but the reserved one while it is not taken over.</summary>
    private IQueryable<Account> Logins() => Set.Where(a => a.PasswordHash != null);

    /// <summary>Selects only the listed columns, so the password hash and the security stamp are never read.</summary>
    private static readonly Expression<Func<Account, LoginSummary>> ToSummary =
        a => new LoginSummary(a.Id, a.Name, a.IsAdmin, a.CreatedAt, a.LastSignedInAt);
}

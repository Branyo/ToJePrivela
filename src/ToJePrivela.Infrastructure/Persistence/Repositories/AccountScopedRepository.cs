using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

/// <summary>Every read is filtered by the owning account's <c>AccountId</c>.</summary>
public abstract class AccountScopedRepository<TEntity> : IAccountScopedRepository<TEntity> where TEntity : class
{
    protected AccountScopedRepository(ToJePrivelaDbContext context)
    {
        Context = context;
    }

    protected ToJePrivelaDbContext Context { get; }

    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(int accountId, int id, CancellationToken cancellationToken = default) =>
        await Owned(accountId).FirstOrDefaultAsync(entity => EF.Property<int>(entity, "Id") == id, cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(int accountId, CancellationToken cancellationToken = default) =>
        await Owned(accountId).ToListAsync(cancellationToken);

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public virtual void Remove(TEntity entity) => Set.Remove(entity);

    /// <summary>The account's entities; what every query of a derived repository starts from.</summary>
    protected virtual IQueryable<TEntity> Owned(int accountId) =>
        Set.Where(entity => EF.Property<int>(entity, "AccountId") == accountId);
}

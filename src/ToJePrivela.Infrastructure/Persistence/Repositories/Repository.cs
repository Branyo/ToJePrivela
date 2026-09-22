using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;

namespace ToJePrivela.Infrastructure.Persistence.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected Repository(ToJePrivelaDbContext context)
    {
        Context = context;
    }

    protected ToJePrivelaDbContext Context { get; }

    protected DbSet<TEntity> Set => Context.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Set.ToListAsync(cancellationToken);

    public virtual async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        await GetByIdAsync(id, cancellationToken) is not null;

    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public virtual void Remove(TEntity entity) => Set.Remove(entity);
}

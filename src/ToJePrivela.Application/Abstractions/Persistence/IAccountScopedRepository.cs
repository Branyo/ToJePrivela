namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>
/// Entities that belong to one account (login). Every read names the account, so a use case can never reach another
/// login's data: an id that exists under another account is simply not found.
/// </summary>
public interface IAccountScopedRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(int accountId, int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(int accountId, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Remove(TEntity entity);
}

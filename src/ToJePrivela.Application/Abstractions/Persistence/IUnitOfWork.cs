namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    /// <exception cref="UniqueConstraintException">A unique index rejected the changes.</exception>
    /// <exception cref="ConcurrencyConflictException">A row changed since it was read.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

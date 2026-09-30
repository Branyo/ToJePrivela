namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    /// <exception cref="UniqueConstraintException">A unique index rejected the changes.</exception>
    /// <exception cref="ConcurrencyConflictException">A row changed since it was read.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Drops every change not saved yet, so entities read afterwards carry the stored values again
    /// (e.g. to retry a change after a <see cref="ConcurrencyConflictException"/>).
    /// </summary>
    void DiscardChanges();
}

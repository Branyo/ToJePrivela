namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    /// <exception cref="UniqueConstraintException">A unique index rejected the changes.</exception>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

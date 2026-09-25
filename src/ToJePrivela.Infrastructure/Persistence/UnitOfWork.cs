using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ToJePrivela.Application.Abstractions.Persistence;

namespace ToJePrivela.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    /// <summary>SQLITE_CONSTRAINT_UNIQUE, the extended result code of a unique index violation.</summary>
    private const int SqliteUniqueConstraintViolation = 2067;

    private readonly ToJePrivelaDbContext _context;

    public UnitOfWork(ToJePrivelaDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("A row changed since it was read.", exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException { SqliteExtendedErrorCode: SqliteUniqueConstraintViolation })
        {
            throw new UniqueConstraintException("A unique index rejected the changes.", exception);
        }
    }
}

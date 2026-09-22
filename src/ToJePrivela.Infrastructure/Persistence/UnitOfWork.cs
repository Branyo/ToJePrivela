using ToJePrivela.Application.Abstractions.Persistence;

namespace ToJePrivela.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ToJePrivelaDbContext _context;

    public UnitOfWork(ToJePrivelaDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}

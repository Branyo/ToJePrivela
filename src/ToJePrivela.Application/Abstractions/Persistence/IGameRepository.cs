using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

public interface IGameRepository : IRepository<Game>
{
    Task<Game?> GetWithDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Game>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);
}

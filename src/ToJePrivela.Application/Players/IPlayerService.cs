using ToJePrivela.Application.Common;
using ToJePrivela.Application.Players.Dtos;

namespace ToJePrivela.Application.Players;

public interface IPlayerService
{
    Task<Result<IReadOnlyList<PlayerDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<PlayerDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<PlayerDto>> CreateAsync(CreatePlayerRequest request, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(int id, UpdatePlayerRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

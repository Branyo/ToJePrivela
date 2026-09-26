using ToJePrivela.Application.Common;
using ToJePrivela.Application.Games.Dtos;

namespace ToJePrivela.Application.Games;

public interface IGameService
{
    Task<Result<IReadOnlyList<GameDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Result<GameDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<GameDetailsDto>> GetDetailsAsync(int id, CancellationToken cancellationToken = default);

    Task<Result<GameDto>> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(int id, UpdateGameRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gives the player a bad card worth the question's bad points; may finish the game.</summary>
    Task<Result<GameDetailsDto>> AwardBadCardAsync(int id, AwardBadCardRequest request, CancellationToken cancellationToken = default);

    /// <summary>Ends a running game early, e.g. when the players stop before anyone hits the limit.</summary>
    Task<Result<GameDetailsDto>> FinishAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

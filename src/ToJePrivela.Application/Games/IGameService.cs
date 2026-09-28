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

    /// <summary>Credits the player with a double that held (one bad point off); never finishes the game.</summary>
    Task<Result<GameDetailsDto>> AwardDoubleAsync(int id, AwardDoubleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Takes back one double tapped by mistake; a conflict when the player has none.</summary>
    Task<Result<GameDetailsDto>> RemoveDoubleAsync(int id, int playerId, CancellationToken cancellationToken = default);

    /// <summary>Ends a running game early, e.g. when the players stop before anyone hits the limit.</summary>
    Task<Result<GameDetailsDto>> FinishAsync(int id, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}

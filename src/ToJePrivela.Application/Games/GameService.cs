using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Games.Mapping;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Games;

public sealed class GameService : IGameService
{
    private readonly IGameRepository _games;
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public GameService(
        IGameRepository games,
        IPlayerRepository players,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _games = games;
        _players = players;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<GameDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var games = await _games.GetAllWithDetailsAsync(cancellationToken);
        return Result.Success(GameMapper.ToDtos(games));
    }

    public async Task<Result<GameDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var game = await _games.GetWithDetailsAsync(id, cancellationToken);

        return game is null
            ? Result.Failure<GameDto>(GameErrors.NotFound(id))
            : Result.Success(GameMapper.ToDto(game));
    }

    public async Task<Result<GameDetailsDto>> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var game = await _games.GetWithDetailsAsync(id, cancellationToken);

        return game is null
            ? Result.Failure<GameDetailsDto>(GameErrors.NotFound(id))
            : Result.Success(GameMapper.ToDetailsDto(game));
    }

    public async Task<Result<GameDto>> CreateAsync(CreateGameRequest request, CancellationToken cancellationToken = default)
    {
        var requestedIds = request.PlayerIds.Distinct().ToList();
        var existingIds = await _players.GetExistingIdsAsync(requestedIds, cancellationToken);
        var missingIds = requestedIds.Except(existingIds).ToList();

        if (missingIds.Count > 0)
        {
            return Result.Failure<GameDto>(GameErrors.UnknownPlayers(missingIds));
        }

        var game = new Game(requestedIds, _timeProvider.GetUtcNow().UtcDateTime);

        await _games.AddAsync(game, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(GameMapper.ToDto(game));
    }

    public async Task<Result> UpdateAsync(int id, UpdateGameRequest request, CancellationToken cancellationToken = default)
    {
        var game = await _games.GetByIdAsync(id, cancellationToken);

        if (game is null)
        {
            return Result.Failure(GameErrors.NotFound(id));
        }

        game.Reschedule(request.Started, request.Finished);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var game = await _games.GetByIdAsync(id, cancellationToken);

        if (game is null)
        {
            return Result.Failure(GameErrors.NotFound(id));
        }

        _games.Remove(game);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

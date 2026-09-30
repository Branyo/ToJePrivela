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
    private readonly IQuestionRepository _questions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public GameService(
        IGameRepository games,
        IPlayerRepository players,
        IQuestionRepository questions,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _games = games;
        _players = players;
        _questions = questions;
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
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure<GameDto>(invalid);
        }

        var requestedIds = request.PlayerIds.Distinct().ToList();
        var existingIds = await _players.GetExistingIdsAsync(requestedIds, cancellationToken);
        var missingIds = requestedIds.Except(existingIds).ToList();

        if (missingIds.Count > 0)
        {
            return Result.Failure<GameDto>(GameErrors.UnknownPlayers(missingIds));
        }

        var game = new Game(
            requestedIds,
            _timeProvider.GetUtcNow().UtcDateTime,
            request.BadCardLimit ?? Game.DefaultBadCardLimit,
            request.BadPointsMode ?? BadPointsMode.Question);

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

        if (request.Started is not DateTime started)
        {
            return Result.Failure(GameErrors.StartRequired(id));
        }

        if (game.IsFinished && request.Finished is null)
        {
            return Result.Failure(GameErrors.CannotReopen(id));
        }

        game.Reschedule(started, request.Finished);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<GameDetailsDto>> AwardBadCardAsync(
        int id,
        AwardBadCardRequest request,
        CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure<GameDetailsDto>(invalid);
        }

        var game = await _games.GetWithDetailsAsync(id, cancellationToken);

        if (game is null)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.NotFound(id));
        }

        if (game.IsFinished)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.AlreadyFinished(id));
        }

        if (!game.HasPlayer(request.PlayerId))
        {
            return Result.Failure<GameDetailsDto>(GameErrors.PlayerNotInGame(id, request.PlayerId));
        }

        if (game.RequiresChosenBadPoints != request.BadPoints.HasValue)
        {
            return Result.Failure<GameDetailsDto>(game.RequiresChosenBadPoints
                ? GameErrors.BadPointsRequired(id)
                : GameErrors.BadPointsNotAllowed(id));
        }

        var question = await _questions.GetByIdAsync(request.QuestionId, cancellationToken);

        if (question is null)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.UnknownQuestion(request.QuestionId));
        }

        game.AwardBadCard(request.PlayerId, question, request.BadPoints, _timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(GameMapper.ToDetailsDto(game));
    }

    public async Task<Result<GameDetailsDto>> AwardDoubleAsync(
        int id,
        AwardDoubleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure<GameDetailsDto>(invalid);
        }

        var game = await _games.GetWithDetailsAsync(id, cancellationToken);

        if (game is null)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.NotFound(id));
        }

        if (game.IsFinished)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.AlreadyFinished(id));
        }

        if (!game.HasPlayer(request.PlayerId))
        {
            return Result.Failure<GameDetailsDto>(GameErrors.PlayerNotInGame(id, request.PlayerId));
        }

        game.AwardDouble(request.PlayerId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(GameMapper.ToDetailsDto(game));
    }

    public async Task<Result<GameDetailsDto>> RemoveDoubleAsync(
        int id,
        int playerId,
        CancellationToken cancellationToken = default)
    {
        var game = await _games.GetWithDetailsAsync(id, cancellationToken);

        if (game is null)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.NotFound(id));
        }

        if (game.IsFinished)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.AlreadyFinished(id));
        }

        var gamePlayer = game.GamePlayers.FirstOrDefault(gp => gp.PlayerId == playerId);

        if (gamePlayer is null)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.PlayerNotInGame(id, playerId));
        }

        if (gamePlayer.Doubles == 0)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.NoDoubleToRemove(id, playerId));
        }

        game.RemoveDouble(playerId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(GameMapper.ToDetailsDto(game));
    }

    public async Task<Result<GameDetailsDto>> FinishAsync(int id, CancellationToken cancellationToken = default)
    {
        var game = await _games.GetWithDetailsAsync(id, cancellationToken);

        if (game is null)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.NotFound(id));
        }

        if (game.IsFinished)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.AlreadyFinished(id));
        }

        game.Finish(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(GameMapper.ToDetailsDto(game));
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

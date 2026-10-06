using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Games.Mapping;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Games;

/// <summary>
/// Works on the signed-in account's games only. A new game is saved under that account and may seat only its players;
/// the questions are shared by every account.
/// </summary>
public sealed class GameService : IGameService
{
    private readonly IGameRepository _games;
    private readonly IPlayerRepository _players;
    private readonly IQuestionRepository _questions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentAccount _currentAccount;

    public GameService(
        IGameRepository games,
        IPlayerRepository players,
        IQuestionRepository questions,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ICurrentAccount currentAccount)
    {
        _games = games;
        _players = players;
        _questions = questions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _currentAccount = currentAccount;
    }

    public async Task<Result<IReadOnlyList<GameDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var games = await _games.GetAllAsync(_currentAccount.Id, cancellationToken);
        return Result.Success(GameMapper.ToDtos(games));
    }

    public async Task<Result<GameDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var game = await _games.GetByIdAsync(_currentAccount.Id, id, cancellationToken);

        return game is null
            ? Result.Failure<GameDto>(GameErrors.NotFound(id))
            : Result.Success(GameMapper.ToDto(game));
    }

    public async Task<Result<GameDetailsDto>> GetDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        var game = await _games.GetByIdAsync(_currentAccount.Id, id, cancellationToken);

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
        var existingIds = await _players.GetExistingIdsAsync(_currentAccount.Id, requestedIds, cancellationToken);
        var missingIds = requestedIds.Except(existingIds).ToList();

        if (missingIds.Count > 0)
        {
            return Result.Failure<GameDto>(GameErrors.UnknownPlayers(missingIds));
        }

        var game = new Game(
            _currentAccount.Id,
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
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure(invalid);
        }

        var game = await _games.GetByIdAsync(_currentAccount.Id, id, cancellationToken);

        if (game is null)
        {
            return Result.Failure(GameErrors.NotFound(id));
        }

        if (request.Started is not DateTime started)
        {
            return Result.Failure(GameErrors.StartRequired(id));
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (game.CheckReschedule(started, request.Finished, now) is { } violation)
        {
            return Result.Failure(GameErrors.From(violation, id));
        }

        game.Reschedule(started, request.Finished, now);
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

        var game = await _games.GetByIdAsync(_currentAccount.Id, id, cancellationToken);

        if (game is null)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.NotFound(id));
        }

        if (game.CheckAwardBadCard(request.PlayerId, request.BadPoints) is { } violation)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.From(violation, id, request.PlayerId));
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
        CancellationToken cancellationToken = default) =>
        RequestValidator.Validate(request) is { } invalid
            ? Result.Failure<GameDetailsDto>(invalid)
            : await ChangeAsync(
                id,
                game => game.CheckAwardDouble(request.PlayerId),
                game => game.AwardDouble(request.PlayerId),
                request.PlayerId,
                cancellationToken);

    public Task<Result<GameDetailsDto>> RemoveDoubleAsync(
        int id,
        int playerId,
        CancellationToken cancellationToken = default) =>
        ChangeAsync(
            id,
            game => game.CheckRemoveDouble(playerId),
            game => game.RemoveDouble(playerId),
            playerId,
            cancellationToken);

    public Task<Result<GameDetailsDto>> FinishAsync(int id, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return ChangeAsync(id, game => game.CheckFinish(now), game => game.Finish(now), playerId: null, cancellationToken);
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var game = await _games.GetByIdAsync(_currentAccount.Id, id, cancellationToken);

        if (game is null)
        {
            return Result.Failure(GameErrors.NotFound(id));
        }

        _games.Remove(game);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Loads the game, lets the domain check the rule, applies the change and saves: the shape every
    /// in-game action shares, so the rule itself lives only in <see cref="Game"/>.
    /// </summary>
    private async Task<Result<GameDetailsDto>> ChangeAsync(
        int id,
        Func<Game, GameRuleViolation?> check,
        Action<Game> change,
        int? playerId,
        CancellationToken cancellationToken)
    {
        var game = await _games.GetByIdAsync(_currentAccount.Id, id, cancellationToken);

        if (game is null)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.NotFound(id));
        }

        if (check(game) is { } violation)
        {
            return Result.Failure<GameDetailsDto>(GameErrors.From(violation, id, playerId));
        }

        change(game);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(GameMapper.ToDetailsDto(game));
    }
}

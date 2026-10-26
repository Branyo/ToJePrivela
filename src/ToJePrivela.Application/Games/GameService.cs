using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Accounts;
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
    private readonly IAccountRepository _accounts;

    public GameService(
        IGameRepository games,
        IPlayerRepository players,
        IQuestionRepository questions,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ICurrentAccount currentAccount,
        IAccountRepository accounts)
    {
        _games = games;
        _players = players;
        _questions = questions;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _currentAccount = currentAccount;
        _accounts = accounts;
    }

    public Task<Result<GameHistoryDto>> GetHistoryAsync(GameHistoryFilter filter, CancellationToken cancellationToken = default) =>
        GameReads.GetHistoryAsync(_games, _currentAccount.Id, filter, cancellationToken);

    public async Task<Result<GameDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var game = await _games.GetByIdAsync(_currentAccount.Id, id, cancellationToken);

        return game is null
            ? Result.Failure<GameDto>(GameErrors.NotFound(id))
            : Result.Success(GameMapper.ToDto(game));
    }

    public Task<Result<GameDetailsDto>> GetDetailsAsync(int id, CancellationToken cancellationToken = default) =>
        GameReads.GetDetailsAsync(_games, _currentAccount.Id, id, cancellationToken);

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

        if (request.Started is not DateTime started)
        {
            return Result.Failure(GameErrors.StartRequired(id));
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var result = await SaveChangeAsync(
            id,
            game =>
            {
                if (game.CheckReschedule(started, request.Finished, now) is { } violation)
                {
                    return Task.FromResult<Error?>(GameErrors.From(violation, id));
                }

                game.Reschedule(started, request.Finished, now);
                return Task.FromResult<Error?>(null);
            },
            cancellationToken);

        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
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

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        return ToDetails(await SaveChangeAsync(
            id,
            async game =>
            {
                if (game.CheckAwardBadCard(request.PlayerId, request.CalledByPlayerId, request.BadPoints) is { } violation)
                {
                    return GameErrors.From(violation, id, request.PlayerId, request.CalledByPlayerId);
                }

                // A chooser game takes the card's worth from the request, so a question deleted since it was shown
                // does not cost the round its card.
                var question = game.RequiresChosenBadPoints
                    ? null
                    : await _questions.GetByIdAsync(request.QuestionId, cancellationToken);

                if (question is null && !game.RequiresChosenBadPoints)
                {
                    return GameErrors.UnknownQuestion(request.QuestionId);
                }

                game.AwardBadCard(request.PlayerId, request.CalledByPlayerId, question, request.BadPoints, now);
                return null;
            },
            cancellationToken));
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

    public async Task<Result<GameDetailsDto>> RecordCallAsync(
        int id,
        RecordCallRequest request,
        CancellationToken cancellationToken = default) =>
        RequestValidator.Validate(request) is { } invalid
            ? Result.Failure<GameDetailsDto>(invalid)
            : await ChangeAsync(
                id,
                game => game.CheckRecordCall(request.PlayerId),
                game => game.RecordCall(request.PlayerId),
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
        // The delete is checked against the game's row version too, so a card or double saved meanwhile is retried
        // like any other change instead of failing the delete.
        var result = await SaveChangeAsync(
            id,
            game =>
            {
                _games.Remove(game);
                return Task.FromResult<Error?>(null);
            },
            cancellationToken);

        return result.IsSuccess ? Result.Success() : Result.Failure(result.Error);
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
        CancellationToken cancellationToken) =>
        ToDetails(await SaveChangeAsync(
            id,
            game =>
            {
                if (check(game) is { } violation)
                {
                    return Task.FromResult<Error?>(GameErrors.From(violation, id, playerId));
                }

                change(game);
                return Task.FromResult<Error?>(null);
            },
            cancellationToken));

    /// <summary>
    /// Loads the game, lets <paramref name="change"/> check and change it (or return why it may not) and saves,
    /// through <see cref="ConcurrencyRetry"/>: a change racing another request is made again on the game as it is now,
    /// so a game finished meanwhile gives <c>Game.AlreadyFinished</c> and two independent changes both stand. A game gone
    /// because an admin deleted the whole login is <see cref="AccountErrors.UnknownAccount"/>, not <c>Game.NotFound</c>.
    /// </summary>
    private Task<Result<Game>> SaveChangeAsync(
        int id,
        Func<Game, Task<Error?>> change,
        CancellationToken cancellationToken) =>
        ConcurrencyRetry.SaveAsync(
            _unitOfWork,
            async () =>
            {
                var game = await _games.GetByIdAsync(_currentAccount.Id, id, cancellationToken);

                if (game is null)
                {
                    return Result.Failure<Game>(await SignedInLogin.GoneOrAsync(
                        _accounts,
                        _currentAccount.Id,
                        GameErrors.NotFound(id),
                        cancellationToken));
                }

                return await change(game) is { } error
                    ? Result.Failure<Game>(error)
                    : Result.Success(game);
            },
            cancellationToken);

    private static Result<GameDetailsDto> ToDetails(Result<Game> result) =>
        result.IsSuccess
            ? Result.Success(GameMapper.ToDetailsDto(result.Value))
            : Result.Failure<GameDetailsDto>(result.Error);
}

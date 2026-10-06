using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Players.Mapping;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Players;

/// <summary>
/// Works on the signed-in account's players only; names are unique within the account, which keeps at most
/// <see cref="Account.MaxPlayers"/> of them.
/// </summary>
public sealed class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _players;
    private readonly IGameRepository _games;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAvatarPicker _avatarPicker;
    private readonly TimeProvider _timeProvider;
    private readonly ICurrentAccount _currentAccount;

    public PlayerService(
        IPlayerRepository players,
        IGameRepository games,
        IUnitOfWork unitOfWork,
        IAvatarPicker avatarPicker,
        TimeProvider timeProvider,
        ICurrentAccount currentAccount)
    {
        _players = players;
        _games = games;
        _unitOfWork = unitOfWork;
        _avatarPicker = avatarPicker;
        _timeProvider = timeProvider;
        _currentAccount = currentAccount;
    }

    public async Task<Result<IReadOnlyList<PlayerDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var players = await _players.GetAllAsync(_currentAccount.Id, cancellationToken);
        return Result.Success(PlayerMapper.ToDtos(players));
    }

    public async Task<Result<PlayerDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetByIdAsync(_currentAccount.Id, id, cancellationToken);

        return player is null
            ? Result.Failure<PlayerDto>(PlayerErrors.NotFound(id))
            : Result.Success(PlayerMapper.ToDto(player));
    }

    public async Task<Result<PlayerDto>> CreateAsync(CreatePlayerRequest request, CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure<PlayerDto>(invalid);
        }

        var name = Player.NormalizeName(request.Name);

        if (await _players.GetByNameAsync(_currentAccount.Id, name, cancellationToken) is not null)
        {
            return Result.Failure<PlayerDto>(PlayerErrors.NameTaken(name));
        }

        if (await _players.CountAsync(_currentAccount.Id, cancellationToken) >= Account.MaxPlayers)
        {
            return Result.Failure<PlayerDto>(PlayerErrors.LimitReached);
        }

        var avatar = _avatarPicker.Pick(await _players.GetAvatarsAsync(_currentAccount.Id, cancellationToken));
        var player = PlayerMapper.ToEntity(request, _currentAccount.Id, avatar);

        await _players.AddAsync(player, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            // Another request took the same name between the check above and this save.
            return Result.Failure<PlayerDto>(PlayerErrors.NameTaken(name));
        }

        return Result.Success(PlayerMapper.ToDto(player));
    }

    public async Task<Result> UpdateAsync(int id, UpdatePlayerRequest request, CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure(invalid);
        }

        var player = await _players.GetByIdAsync(_currentAccount.Id, id, cancellationToken);

        if (player is null)
        {
            return Result.Failure(PlayerErrors.NotFound(id));
        }

        var name = Player.NormalizeName(request.Name);
        var duplicate = await _players.GetByNameAsync(_currentAccount.Id, name, cancellationToken);

        if (duplicate is not null && duplicate.Id != id)
        {
            return Result.Failure(PlayerErrors.NameTaken(name));
        }

        player.Rename(name);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            // Another request took the same name between the check above and this save.
            return Result.Failure(PlayerErrors.NameTaken(name));
        }

        return Result.Success();
    }

    /// <summary>
    /// Deletes the player but keeps every game they played: their seat becomes an unknown player, and a game
    /// still running is cancelled, since it cannot go on without them.
    /// </summary>
    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetByIdAsync(_currentAccount.Id, id, cancellationToken);

        if (player is null)
        {
            return Result.Failure(PlayerErrors.NotFound(id));
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var game in await _games.GetByPlayerAsync(_currentAccount.Id, id, cancellationToken))
        {
            game.ForgetPlayer(id, now);
        }

        _players.Remove(player);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

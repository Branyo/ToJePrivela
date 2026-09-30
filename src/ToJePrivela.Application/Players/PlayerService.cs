using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Players.Mapping;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Players;

public sealed class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAvatarPicker _avatarPicker;

    public PlayerService(IPlayerRepository players, IUnitOfWork unitOfWork, IAvatarPicker avatarPicker)
    {
        _players = players;
        _unitOfWork = unitOfWork;
        _avatarPicker = avatarPicker;
    }

    public async Task<Result<IReadOnlyList<PlayerDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var players = await _players.GetAllAsync(cancellationToken);
        return Result.Success(PlayerMapper.ToDtos(players));
    }

    public async Task<Result<PlayerDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetByIdAsync(id, cancellationToken);

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

        if (await _players.GetByNameAsync(name, cancellationToken) is not null)
        {
            return Result.Failure<PlayerDto>(PlayerErrors.NameTaken(name));
        }

        var avatar = _avatarPicker.Pick(await _players.GetAvatarsAsync(cancellationToken));
        var player = PlayerMapper.ToEntity(request, avatar);

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

        var player = await _players.GetByIdAsync(id, cancellationToken);

        if (player is null)
        {
            return Result.Failure(PlayerErrors.NotFound(id));
        }

        var name = Player.NormalizeName(request.Name);
        var duplicate = await _players.GetByNameAsync(name, cancellationToken);

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

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetByIdAsync(id, cancellationToken);

        if (player is null)
        {
            return Result.Failure(PlayerErrors.NotFound(id));
        }

        _players.Remove(player);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

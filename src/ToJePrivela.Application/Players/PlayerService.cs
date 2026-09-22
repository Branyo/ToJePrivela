using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Players.Mapping;

namespace ToJePrivela.Application.Players;

public sealed class PlayerService : IPlayerService
{
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _unitOfWork;

    public PlayerService(IPlayerRepository players, IUnitOfWork unitOfWork)
    {
        _players = players;
        _unitOfWork = unitOfWork;
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
        if (await _players.GetByNameAsync(request.Name, cancellationToken) is not null)
        {
            return Result.Failure<PlayerDto>(PlayerErrors.NameTaken(request.Name));
        }

        var player = PlayerMapper.ToEntity(request);

        await _players.AddAsync(player, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(PlayerMapper.ToDto(player));
    }

    public async Task<Result> UpdateAsync(int id, UpdatePlayerRequest request, CancellationToken cancellationToken = default)
    {
        var player = await _players.GetByIdAsync(id, cancellationToken);

        if (player is null)
        {
            return Result.Failure(PlayerErrors.NotFound(id));
        }

        var duplicate = await _players.GetByNameAsync(request.Name, cancellationToken);

        if (duplicate is not null && duplicate.Id != id)
        {
            return Result.Failure(PlayerErrors.NameTaken(request.Name));
        }

        player.Rename(request.Name);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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

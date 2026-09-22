using NSubstitute;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Application.Players;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Players;

public class PlayerServiceTests
{
    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly PlayerService _sut;

    public PlayerServiceTests()
    {
        _sut = new PlayerService(_players, _unitOfWork);
    }

    [Fact]
    public async Task GetAllAsync_MapsEveryPlayer()
    {
        _players.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([new Player("Brano"), new Player("Duri")]);

        var result = await _sut.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(["Brano", "Duri"], result.Value.Select(p => p.Name));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFoundForUnknownPlayer()
    {
        _players.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Player?)null);

        var result = await _sut.GetByIdAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_StoresThePlayer()
    {
        _players.GetByNameAsync("Brano", Arg.Any<CancellationToken>()).Returns((Player?)null);

        var result = await _sut.CreateAsync(new CreatePlayerRequest { Name = "Brano" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Brano", result.Value.Name);
        await _players.Received(1).AddAsync(Arg.Is<Player>(p => p.Name == "Brano"), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateName()
    {
        _players.GetByNameAsync("Brano", Arg.Any<CancellationToken>()).Returns(new Player("Brano"));

        var result = await _sut.CreateAsync(new CreatePlayerRequest { Name = "Brano" });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _players.DidNotReceive().AddAsync(Arg.Any<Player>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFoundForUnknownPlayer()
    {
        _players.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Player?)null);

        var result = await _sut.UpdateAsync(7, new UpdatePlayerRequest { Name = "Duri" });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task UpdateAsync_RejectsNameOwnedByAnotherPlayer()
    {
        var player = TestEntities.Player(1, "Brano");
        var other = TestEntities.Player(2, "Duri");

        _players.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(player);
        _players.GetByNameAsync("Duri", Arg.Any<CancellationToken>()).Returns(other);

        var result = await _sut.UpdateAsync(1, new UpdatePlayerRequest { Name = "Duri" });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Brano", player.Name);
    }

    [Fact]
    public async Task UpdateAsync_AllowsPlayerToKeepOwnName()
    {
        var player = TestEntities.Player(1, "Brano");

        _players.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(player);
        _players.GetByNameAsync("Brano", Arg.Any<CancellationToken>()).Returns(player);

        var result = await _sut.UpdateAsync(1, new UpdatePlayerRequest { Name = "Brano" });

        Assert.True(result.IsSuccess);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_RenamesThePlayer()
    {
        var player = TestEntities.Player(1, "Brano");

        _players.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(player);
        _players.GetByNameAsync("Jozo", Arg.Any<CancellationToken>()).Returns((Player?)null);

        var result = await _sut.UpdateAsync(1, new UpdatePlayerRequest { Name = "Jozo" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Jozo", player.Name);
    }

    [Fact]
    public async Task DeleteAsync_RemovesThePlayer()
    {
        var player = TestEntities.Player(1, "Brano");
        _players.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(player);

        var result = await _sut.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _players.Received(1).Remove(player);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFoundForUnknownPlayer()
    {
        _players.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Player?)null);

        var result = await _sut.DeleteAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        _players.DidNotReceive().Remove(Arg.Any<Player>());
    }
}

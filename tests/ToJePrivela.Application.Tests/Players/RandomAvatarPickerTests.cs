using ToJePrivela.Application.Players;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Players;

public class RandomAvatarPickerTests
{
    private readonly RandomAvatarPicker _sut = new();

    [Fact]
    public void Pick_ReturnsAnAvatarFromThePool()
    {
        Assert.Contains(_sut.Pick([]), PlayerAvatars.All);
    }

    [Fact]
    public void Pick_PrefersTheOnlyUnusedAvatar()
    {
        var unused = PlayerAvatars.All[^1];
        var inUse = PlayerAvatars.All.Where(avatar => avatar != unused).ToList();

        for (var i = 0; i < 20; i++)
        {
            Assert.Equal(unused, _sut.Pick(inUse));
        }
    }

    [Fact]
    public void Pick_PrefersTheLeastUsedAvatarsOnceAllAreTaken()
    {
        var once = PlayerAvatars.All[0];
        var inUse = PlayerAvatars.All.Concat(PlayerAvatars.All.Where(avatar => avatar != once)).ToList();

        Assert.Equal(once, _sut.Pick(inUse));
    }
}

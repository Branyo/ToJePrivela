using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Domain.Tests.Entities;

public class PlayerTests
{
    [Fact]
    public void Constructor_TrimsTheName()
    {
        var player = new Player(TestAccountId, "  Brano  ", "🦊");

        Assert.Equal("Brano", player.Name);
    }

    [Fact]
    public void Constructor_StartsWithoutGames()
    {
        var player = new Player(TestAccountId, "Brano", "🦊");

        Assert.Empty(player.GamePlayers);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void Constructor_RejectsTooShortName(string name)
    {
        Assert.Throws<DomainException>(() => new Player(TestAccountId, name, "🦊"));
    }

    [Fact]
    public void Constructor_RejectsTooLongName()
    {
        var name = new string('x', Player.NameMaxLength + 1);

        Assert.Throws<DomainException>(() => new Player(TestAccountId, name, "🦊"));
    }

    [Fact]
    public void Constructor_KeepsTheAvatar()
    {
        var player = new Player(TestAccountId, "Brano", "🐼");

        Assert.Equal("🐼", player.Avatar);
    }

    [Theory]
    [InlineData("")]
    [InlineData("X")]
    [InlineData("🫏")]
    public void Constructor_RejectsAvatarOutsideThePool(string avatar)
    {
        Assert.Throws<DomainException>(() => new Player(TestAccountId, "Brano", avatar));
    }

    [Fact]
    public void Avatars_AreUnique()
    {
        Assert.Equal(PlayerAvatars.All.Count, PlayerAvatars.All.Distinct().Count());
        Assert.All(PlayerAvatars.All, avatar => Assert.InRange(avatar.Length, 1, PlayerAvatars.MaxLength));
    }

    [Fact]
    public void Rename_ReplacesTheName()
    {
        var player = new Player(TestAccountId, "Brano", "🦊");

        player.Rename("Duri");

        Assert.Equal("Duri", player.Name);
    }

    [Fact]
    public void NameKey_FollowsTheName()
    {
        var player = new Player(TestAccountId, "Štefan", "🦊");
        Assert.Equal("štefan", player.NameKey);

        player.Rename("ĽUBO");
        Assert.Equal("ľubo", player.NameKey);
    }

    [Fact]
    public void Rename_RejectsInvalidName()
    {
        var player = new Player(TestAccountId, "Brano", "🦊");

        Assert.Throws<DomainException>(() => player.Rename("X"));
        Assert.Equal("Brano", player.Name);
    }

    [Fact]
    public void Constructor_KeepsTheOwningAccount()
    {
        Assert.Equal(7, new Player(7, "Brano", "🦊").AccountId);
    }

    [Fact]
    public void Constructor_RejectsAMissingAccount()
    {
        Assert.Throws<DomainException>(() => new Player(0, "Brano", "🦊"));
    }
}

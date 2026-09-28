using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

public class Player
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 50;

    private readonly List<GamePlayer> _gamePlayers = [];

    private Player()
    {
        Name = string.Empty;
        Avatar = string.Empty;
    }

    public Player(string name, string avatar)
    {
        Name = Guard.AgainstInvalidLength(name, nameof(name), NameMinLength, NameMaxLength);

        if (!PlayerAvatars.IsValid(avatar))
        {
            throw new DomainException($"{nameof(avatar)} must be one of the player avatars.");
        }

        Avatar = avatar;
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    /// <summary>One of <see cref="PlayerAvatars.All"/>, given once at creation and never changed.</summary>
    public string Avatar { get; private set; }

    public IReadOnlyCollection<GamePlayer> GamePlayers => _gamePlayers.AsReadOnly();

    public void Rename(string name)
    {
        Name = Guard.AgainstInvalidLength(name, nameof(name), NameMinLength, NameMaxLength);
    }
}

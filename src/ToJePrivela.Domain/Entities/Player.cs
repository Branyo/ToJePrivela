using System.Diagnostics.CodeAnalysis;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

public class Player
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 50;

    /// <summary>The form a name is stored in; <see cref="NameKey"/> is what makes two names the same.</summary>
    public static string NormalizeName(string name) => name.Trim();

    private readonly List<GamePlayer> _gamePlayers = [];

    private Player()
    {
        Name = string.Empty;
        NameKey = string.Empty;
        Avatar = string.Empty;
    }

    public Player(string name, string avatar)
    {
        SetName(name);

        if (!PlayerAvatars.IsValid(avatar))
        {
            throw new DomainException($"{nameof(avatar)} must be one of the player avatars.");
        }

        Avatar = avatar;
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    /// <summary><see cref="Name"/> as <see cref="NameKeys.Of"/> compares it; unique among all players.</summary>
    public string NameKey { get; private set; }

    /// <summary>One of <see cref="PlayerAvatars.All"/>, given once at creation and never changed.</summary>
    public string Avatar { get; private set; }

    public IReadOnlyCollection<GamePlayer> GamePlayers => _gamePlayers.AsReadOnly();

    public void Rename(string name) => SetName(name);

    [MemberNotNull(nameof(Name), nameof(NameKey))]
    private void SetName(string name)
    {
        Name = Guard.AgainstInvalidLength(name, nameof(name), NameMinLength, NameMaxLength);
        NameKey = NameKeys.Of(Name);
    }
}

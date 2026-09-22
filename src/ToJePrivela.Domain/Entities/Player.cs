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
    }

    public Player(string name)
    {
        Name = Guard.AgainstInvalidLength(name, nameof(name), NameMinLength, NameMaxLength);
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    public IReadOnlyCollection<GamePlayer> GamePlayers => _gamePlayers.AsReadOnly();

    public void Rename(string name)
    {
        Name = Guard.AgainstInvalidLength(name, nameof(name), NameMinLength, NameMaxLength);
    }
}

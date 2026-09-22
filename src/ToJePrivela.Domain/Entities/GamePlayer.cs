using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

public class GamePlayer
{
    private GamePlayer()
    {
    }

    public GamePlayer(int playerId, int badPoints = 0)
    {
        PlayerId = playerId;
        BadPoints = Guard.AgainstNegative(badPoints, nameof(badPoints));
    }

    public int GameId { get; private set; }

    public Game? Game { get; private set; }

    public int PlayerId { get; private set; }

    public Player? Player { get; private set; }

    public int BadPoints { get; private set; }
}

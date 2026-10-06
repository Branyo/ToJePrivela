namespace ToJePrivela.Application.Players;

/// <summary>Chooses the avatar of a new player; a port so tests stay deterministic.</summary>
public interface IAvatarPicker
{
    /// <param name="avatarsInUse">Avatars of the account's existing players, one entry per player.</param>
    string Pick(IReadOnlyCollection<string> avatarsInUse);
}

namespace ToJePrivela.Domain.Entities;

/// <summary>The animals a player can be given. The donkey is reserved for bad cards, so it is not one of them.</summary>
public static class PlayerAvatars
{
    public const int MaxLength = 16;

    public static readonly IReadOnlyList<string> All =
    [
        "🦊", "🐸", "🐼", "🐯", "🐙", "🦄", "🐨", "🐧", "🦁", "🐵", "🐰", "🦉",
        "🐶", "🐱", "🐻", "🐷", "🐮", "🐔", "🦋", "🐢", "🦀", "🐳", "🦒", "🦔",
    ];

    public static bool IsValid(string? avatar) => avatar is not null && All.Contains(avatar);
}

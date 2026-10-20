namespace ToJePrivela.Domain.Entities;

/// <summary>
/// The emojis (mostly animals) a player can be given, each with the background colour it is shown on. The colour
/// belongs to its emoji, so it can be changed for one avatar without touching the others; pick one the emoji stands
/// out against. The donkey is reserved for bad cards, so it is not one of them.
/// </summary>
public static class PlayerAvatars
{
    public const int MaxLength = 16;

    // The palette the avatars share; each colour is given to about as many emojis as the others.
    private const string Green = "#3ddc97";
    private const string Orange = "#ff9f1c";
    private const string Sky = "#4cc9f0";
    private const string Pink = "#f15bb5";
    private const string Purple = "#9b5de5";
    private const string Red = "#ff5d5d";
    private const string Mint = "#80ed99";
    private const string Blush = "#ffafcc";
    private const string Lilac = "#c77dff";
    private const string Blue = "#00bbf9";
    private const string Sand = "#f4a261";
    private const string Yellow = "#fee440";

    public static readonly IReadOnlyList<PlayerAvatar> All =
    [
        new("🦊", Green),
        new("🐸", Orange),
        new("🐼", Sky),
        new("🐯", Pink),
        new("🐙", Yellow),
        new("🦄", Red),
        new("🐨", Orange),
        new("🐧", Orange),
        new("🦁", Mint),
        new("🐵", Blush),
        new("🐰", Lilac),
        new("🦉", Blue),
        new("🐶", Blue),
        new("🐱", Green),
        new("🐻", Sky),
        new("🐷", Green),
        new("🐮", Purple),
        new("🐔", Red),
        new("🦋", Sand),
        new("🐢", Yellow),
        new("🦀", Mint),
        new("🐳", Blush),
        new("🦒", Lilac),
        new("🦔", Blue),
        new("🐒", Blue),
        new("🦍", Sand),
        new("🐺", Orange),
        new("🦝", Pink),
        new("🐈", Purple),
        new("🐈‍⬛", Red),
        new("🐅", Blue),
        new("🐆", Sky),
        new("🐴", Mint),
        new("🫎", Blush),
        new("🐎", Lilac),
        new("🦓", Sand),
        new("🦌", Mint),
        new("🦬", Green),
        new("🐂", Sky),
        new("🐃", Pink),
        new("🐄", Purple),
        new("🐏", Red),
        new("🐑", Orange),
        new("🐐", Purple),
        new("🐪", Mint),
        new("🐫", Blush),
        new("🦙", Lilac),
        new("🐘", Sand),
        new("🦣", Sky),
        new("🦏", Red),
        new("🦛", Sky),
        new("🐭", Pink),
        new("🐹", Purple),
        new("🐿️", Green),
        new("🦫", Blue),
        new("🦇", Yellow),
        new("🐻‍❄️", Pink),
        new("🦥", Blush),
        new("🦦", Lilac),
        new("🐦", Sand),
        new("🐓", Yellow),
        new("🐣", Green),
        new("🐤", Sky),
        new("🐥", Pink),
        new("🦘", Purple),
        new("🕊️", Red),
        new("🦅", Blue),
        new("🦆", Yellow),
        new("🦢", Orange),
        new("🦤", Blush),
        new("🦩", Green),
        new("🦚", Sand),
        new("🦜", Orange),
        new("🪿", Lilac),
        new("🐦‍🔥", Sky),
        new("🐌", Pink),
        new("🐛", Purple),
        new("🐜", Red),
        new("🐝", Blue),
        new("🐞", Yellow),
        new("🦗", Blush),
        new("🐊", Blush),
        new("🦎", Lilac),
        new("🐲", Sand),
        new("🦕", Orange),
        new("🦖", Pink),
        new("🐬", Yellow),
        new("🦭", Mint),
        new("🐟", Purple),
        new("🐡", Red),
        new("🦈", Sand),
        new("🦞", Yellow),
        new("🦐", Mint),
        new("🦑", Mint),
        new("⛄", Lilac),
        new("🌛", Green)
    ];

    /// <summary>The emojis of <see cref="All"/>, in the same order.</summary>
    public static readonly IReadOnlyList<string> Emojis = [.. All.Select(avatar => avatar.Emoji)];

    public static bool IsValid(string? avatar) => avatar is not null && Emojis.Contains(avatar);
}

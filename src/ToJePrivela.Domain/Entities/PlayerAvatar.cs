namespace ToJePrivela.Domain.Entities;

/// <summary>One of <see cref="PlayerAvatars.All"/>: the emoji a player is shown as and the background colour behind it.</summary>
/// <param name="Emoji">What <see cref="Player.Avatar"/> stores.</param>
/// <param name="Color">A CSS colour, <c>#rrggbb</c>.</param>
public sealed record PlayerAvatar(string Emoji, string Color);

using ToJePrivela.Application.Rules.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Rules.Mapping;

public static class AvatarMapper
{
    public static AvatarDto ToDto(PlayerAvatar avatar) => new(avatar.Emoji, avatar.Color);

    public static IReadOnlyList<AvatarDto> ToDtos(IEnumerable<PlayerAvatar> avatars) =>
        avatars.Select(ToDto).ToList();
}

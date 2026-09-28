using ToJePrivela.Application.Players;

namespace ToJePrivela.Application.Tests.Common;

public sealed class FixedAvatarPicker : IAvatarPicker
{
    private readonly string _avatar;

    public FixedAvatarPicker(string avatar)
    {
        _avatar = avatar;
    }

    public IReadOnlyCollection<string>? LastAvatarsInUse { get; private set; }

    public string Pick(IReadOnlyCollection<string> avatarsInUse)
    {
        LastAvatarsInUse = avatarsInUse;
        return _avatar;
    }
}

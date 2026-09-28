using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Players;

/// <summary>A random avatar among the least used ones, so players rarely share an animal.</summary>
public sealed class RandomAvatarPicker : IAvatarPicker
{
    public string Pick(IReadOnlyCollection<string> avatarsInUse)
    {
        var uses = avatarsInUse
            .GroupBy(avatar => avatar)
            .ToDictionary(group => group.Key, group => group.Count());

        var fewestUses = PlayerAvatars.All.Min(avatar => uses.GetValueOrDefault(avatar));
        var candidates = PlayerAvatars.All.Where(avatar => uses.GetValueOrDefault(avatar) == fewestUses).ToList();

        return candidates[Random.Shared.Next(candidates.Count)];
    }
}

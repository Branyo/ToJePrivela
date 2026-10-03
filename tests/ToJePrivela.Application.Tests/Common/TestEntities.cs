using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Common;

/// <summary>Entity ids are database-assigned, so tests set them through reflection.</summary>
public static class TestEntities
{
    public static readonly DateTime CreatedAt = new(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);

    public static Player Player(int id, string name) => WithId(new Player(name, "🦊"), id);

    public static Question Question(
        int id,
        string text,
        string answer,
        QuestionCategory category,
        int badPoints = 3,
        QuestionSource source = QuestionSource.Manual) =>
        WithId(new Question(text, answer, category, badPoints, source, CreatedAt), id);

    public static QuestionCategory Category(int id, string name) => WithId(new QuestionCategory(name), id);

    public static Game Game(
        int id,
        IEnumerable<int> playerIds,
        DateTime started,
        int badCardLimit = Domain.Entities.Game.DefaultBadCardLimit,
        BadPointsMode badPointsMode = BadPointsMode.Question) =>
        WithId(new Game(playerIds, started, badCardLimit, badPointsMode), id);

    public static Account Account(int id, string externalId = "sub", bool isAdmin = false)
    {
        var account = WithId(
            Domain.Entities.Account.Register(IdentityProvider.Google, externalId, $"{externalId}@example.com", externalId, CreatedAt),
            id);

        if (isAdmin)
        {
            account.GrantAdmin();
        }

        return account;
    }

    /// <summary>The account the migration inserts; the domain itself never creates one.</summary>
    public static Account ReservedAccount() =>
        WithId((Account)Activator.CreateInstance(typeof(Account), nonPublic: true)!, Domain.Entities.Account.ReservedId);

    private static TEntity WithId<TEntity>(TEntity entity, int id)
    {
        typeof(TEntity).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}

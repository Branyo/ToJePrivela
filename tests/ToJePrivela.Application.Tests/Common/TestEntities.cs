using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Common;

/// <summary>Entity ids are database-assigned, so tests set them through reflection.</summary>
public static class TestEntities
{
    public static readonly DateTime CreatedAt = new(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);

    public static Player Player(int id, string name) => WithId(new Player(TestAccountId, name, "🦊"), id);

    /// <summary>The English text is the Slovak one unless given.</summary>
    public static Question Question(
        int id,
        string textSk,
        string answer,
        QuestionCategory category,
        int badPoints = 3,
        QuestionSource source = QuestionSource.Manual,
        string? textEn = null) =>
        WithId(new Question(textSk, textEn ?? textSk, answer, category, badPoints, source, CreatedAt), id);

    /// <summary>Like a question stored before texts became bilingual; the domain itself never creates one.</summary>
    public static Question WithoutEnglish(Question question)
    {
        typeof(Question).GetProperty(nameof(Domain.Entities.Question.TextEn))!.SetValue(question, null);
        return question;
    }

    /// <summary>The English name is the Slovak one unless given.</summary>
    public static QuestionCategory Category(int id, string nameSk, string? nameEn = null) =>
        WithId(new QuestionCategory(nameSk, nameEn ?? nameSk), id);

    public static Game Game(
        int id,
        IEnumerable<int> playerIds,
        DateTime started,
        int badCardLimit = Domain.Entities.Game.DefaultBadCardLimit,
        BadPointsMode badPointsMode = BadPointsMode.Question) =>
        WithId(new Game(TestAccountId, playerIds, started, badCardLimit, badPointsMode), id);

    public static Account Account(int id, string name = "brano", bool isAdmin = false, string passwordHash = "hash")
    {
        var account = isAdmin
            ? Domain.Entities.Account.CreateAdmin(name, passwordHash, CreatedAt)
            : Domain.Entities.Account.Register(name, passwordHash, CreatedAt);

        return WithId(account, id);
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

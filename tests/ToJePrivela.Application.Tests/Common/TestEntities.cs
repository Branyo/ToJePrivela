using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Common;

/// <summary>Entity ids are database-assigned, so tests set them through reflection.</summary>
public static class TestEntities
{
    public static readonly DateTime CreatedAt = new(2026, 9, 24, 10, 0, 0, DateTimeKind.Utc);

    public static Player Player(int id, string name) => WithId(new Player(name), id);

    public static Question Question(
        int id,
        string text,
        string answer,
        QuestionCategory category,
        int badPoints = 3,
        QuestionSource source = QuestionSource.Manual) =>
        WithId(new Question(text, answer, category, badPoints, source, CreatedAt), id);

    public static QuestionCategory Category(int id, string name, int? addedByPlayerId = null) =>
        WithId(new QuestionCategory(name, addedByPlayerId), id);

    public static Game Game(int id, IEnumerable<int> playerIds, DateTime started) =>
        WithId(new Game(playerIds, started), id);

    private static TEntity WithId<TEntity>(TEntity entity, int id)
    {
        typeof(TEntity).GetProperty("Id")!.SetValue(entity, id);
        return entity;
    }
}

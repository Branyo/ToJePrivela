using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

public class QuestionCategory
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 32;

    private QuestionCategory()
    {
        Name = string.Empty;
    }

    public QuestionCategory(string name, int? addedByPlayerId = null)
    {
        Name = Guard.AgainstInvalidLength(name, nameof(name), NameMinLength, NameMaxLength);
        AddedByPlayerId = addedByPlayerId;
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    public int? AddedByPlayerId { get; private set; }

    public Player? AddedByPlayer { get; private set; }

    public void Rename(string name)
    {
        Name = Guard.AgainstInvalidLength(name, nameof(name), NameMinLength, NameMaxLength);
    }
}

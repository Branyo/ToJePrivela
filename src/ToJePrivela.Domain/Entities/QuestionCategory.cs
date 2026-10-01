using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

/// <summary>Immutable once created: to "rename" a category, delete it and add a new one.</summary>
public class QuestionCategory
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 32;

    /// <summary>The form a name is stored in; <see cref="NameKey"/> is what makes two names the same.</summary>
    public static string NormalizeName(string name) => name.Trim();

    private QuestionCategory()
    {
        Name = string.Empty;
        NameKey = string.Empty;
    }

    public QuestionCategory(string name)
    {
        Name = Guard.AgainstInvalidLength(name, nameof(name), NameMinLength, NameMaxLength);
        NameKey = NameKeys.Of(Name);
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    /// <summary><see cref="Name"/> as <see cref="NameKeys.Of"/> compares it; unique among all categories.</summary>
    public string NameKey { get; private set; }
}

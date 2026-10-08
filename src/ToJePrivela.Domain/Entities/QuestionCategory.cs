using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

/// <summary>
/// Named in Slovak and in English. Immutable once created: to "rename" a category, delete it and add a new one.
/// </summary>
public class QuestionCategory
{
    public const int NameMinLength = 2;
    public const int NameMaxLength = 32;

    /// <summary>The form a name is stored in; <see cref="NameKeys.Of"/> is what makes two names the same.</summary>
    public static string NormalizeName(string name) => name.Trim();

    /// <summary>Whether the constructor would accept <paramref name="name"/> in either language, without throwing.</summary>
    public static bool IsValidName(string? name) =>
        name?.Trim().Length is >= NameMinLength and <= NameMaxLength;

    private QuestionCategory()
    {
        NameSk = string.Empty;
        NameSkKey = string.Empty;
        NameEn = string.Empty;
        NameEnKey = string.Empty;
    }

    public QuestionCategory(string nameSk, string nameEn)
    {
        NameSk = Guard.AgainstInvalidLength(nameSk, nameof(nameSk), NameMinLength, NameMaxLength);
        NameEn = Guard.AgainstInvalidLength(nameEn, nameof(nameEn), NameMinLength, NameMaxLength);
        NameSkKey = NameKeys.Of(NameSk);
        NameEnKey = NameKeys.Of(NameEn);
    }

    public int Id { get; private set; }

    public string NameSk { get; private set; }

    /// <summary><see cref="NameSk"/> as <see cref="NameKeys.Of"/> compares it; unique among all categories.</summary>
    public string NameSkKey { get; private set; }

    public string NameEn { get; private set; }

    /// <summary><see cref="NameEn"/> as <see cref="NameKeys.Of"/> compares it; unique among all categories.</summary>
    public string NameEnKey { get; private set; }

    public string NameIn(Language language) => language == Language.En ? NameEn : NameSk;
}

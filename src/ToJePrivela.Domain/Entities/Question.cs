using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

/// <summary>
/// Written in Slovak and in English. Questions stored before texts became bilingual have no English text yet;
/// they show their Slovak one instead.
/// </summary>
public class Question
{
    public const int TextMinLength = 8;
    public const int TextMaxLength = 512;
    public const int MinBadPoints = 1;
    public const int MaxBadPoints = 5;

    private Question()
    {
        TextSk = string.Empty;
        Answer = string.Empty;
    }

    public Question(
        string textSk,
        string textEn,
        string answer,
        QuestionCategory category,
        int badPoints,
        QuestionSource source,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(category);

        TextSk = Guard.AgainstInvalidLength(textSk, nameof(textSk), TextMinLength, TextMaxLength);
        TextEn = Guard.AgainstInvalidLength(textEn, nameof(textEn), TextMinLength, TextMaxLength);
        Answer = Guard.AgainstNonNumeric(answer, nameof(answer));
        BadPoints = Guard.AgainstOutOfRange(badPoints, nameof(badPoints), MinBadPoints, MaxBadPoints);
        Category = category;
        CategoryId = category.Id;
        Source = source;
        CreatedAt = UtcTime.Normalize(createdAt);
    }

    /// <summary>Whether the constructor would accept <paramref name="text"/> in either language, without throwing.</summary>
    public static bool IsValidText(string? text) =>
        text?.Trim().Length is >= TextMinLength and <= TextMaxLength;

    /// <summary>Whether the constructor would accept <paramref name="answer"/>, without throwing.</summary>
    public static bool IsValidAnswer(string? answer) =>
        !string.IsNullOrWhiteSpace(answer) && Guard.IsNumeric(answer);

    public int Id { get; private set; }

    public string TextSk { get; private set; }

    /// <summary>Null only for a question stored before texts became bilingual.</summary>
    public string? TextEn { get; private set; }

    /// <summary>Stored as text, but always a numeric value.</summary>
    public string Answer { get; private set; }

    public int CategoryId { get; private set; }

    public QuestionCategory? Category { get; private set; }

    /// <summary>Penalty for a wrong answer.</summary>
    public int BadPoints { get; private set; }

    public QuestionSource Source { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>How many times the question has been shown to players.</summary>
    public int ViewCount { get; private set; }

    public DateTime? LastViewedAt { get; private set; }

    /// <summary>
    /// Row version: every change bumps it, so a concurrent change is detected instead of overwritten.
    /// SQLite has no native rowversion, so the entity maintains it itself.
    /// </summary>
    public long Version { get; private set; }

    /// <summary>The text in <paramref name="language"/>; the Slovak one while an English one is missing.</summary>
    public string TextIn(Language language) => language == Language.En ? TextEn ?? TextSk : TextSk;

    /// <summary>
    /// Validates everything before assigning, so a rejected update leaves the question untouched.
    /// A question someone has edited is theirs now, so it becomes <see cref="QuestionSource.Manual"/>.
    /// </summary>
    /// <param name="textEn">Null keeps a question without an English text as it is; it never removes one.</param>
    public void Update(string textSk, string? textEn, string answer, QuestionCategory category, int badPoints)
    {
        ArgumentNullException.ThrowIfNull(category);

        if (textEn is null && TextEn is not null)
        {
            throw new DomainException("textEn must not be removed.");
        }

        var validatedTextSk = Guard.AgainstInvalidLength(textSk, nameof(textSk), TextMinLength, TextMaxLength);
        var validatedTextEn = textEn is null
            ? null
            : Guard.AgainstInvalidLength(textEn, nameof(textEn), TextMinLength, TextMaxLength);
        var validatedAnswer = Guard.AgainstNonNumeric(answer, nameof(answer));
        var validatedBadPoints = Guard.AgainstOutOfRange(badPoints, nameof(badPoints), MinBadPoints, MaxBadPoints);

        TextSk = validatedTextSk;
        TextEn = validatedTextEn;
        Answer = validatedAnswer;
        BadPoints = validatedBadPoints;
        Category = category;
        CategoryId = category.Id;
        Source = QuestionSource.Manual;
        Version++;
    }

    public void MarkViewed(DateTime viewedAt)
    {
        ViewCount++;
        LastViewedAt = UtcTime.Normalize(viewedAt);
        Version++;
    }
}

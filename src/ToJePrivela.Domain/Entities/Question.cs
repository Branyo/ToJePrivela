using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

public class Question
{
    public const int TextMinLength = 8;
    public const int TextMaxLength = 512;
    public const int MinBadPoints = 1;
    public const int MaxBadPoints = 5;

    private Question()
    {
        Text = string.Empty;
        Answer = string.Empty;
    }

    public Question(
        string text,
        string answer,
        QuestionCategory category,
        int badPoints,
        QuestionSource source,
        DateTime createdAt)
    {
        ArgumentNullException.ThrowIfNull(category);

        Text = Guard.AgainstInvalidLength(text, nameof(text), TextMinLength, TextMaxLength);
        Answer = Guard.AgainstNonNumeric(answer, nameof(answer));
        BadPoints = Guard.AgainstOutOfRange(badPoints, nameof(badPoints), MinBadPoints, MaxBadPoints);
        Category = category;
        CategoryId = category.Id;
        Source = source;
        CreatedAt = createdAt;
    }

    public int Id { get; private set; }

    public string Text { get; private set; }

    /// <summary>Stored as text, but always a numeric value.</summary>
    public string Answer { get; private set; }

    public int CategoryId { get; private set; }

    public QuestionCategory? Category { get; private set; }

    /// <summary>Penalty for a wrong answer.</summary>
    public int BadPoints { get; private set; }

    public QuestionSource Source { get; private set; }

    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Validates everything before assigning, so a rejected update leaves the question untouched.
    /// A question someone has edited is theirs now, so it becomes <see cref="QuestionSource.Manual"/>.
    /// </summary>
    public void Update(string text, string answer, QuestionCategory category, int badPoints)
    {
        ArgumentNullException.ThrowIfNull(category);

        var validatedText = Guard.AgainstInvalidLength(text, nameof(text), TextMinLength, TextMaxLength);
        var validatedAnswer = Guard.AgainstNonNumeric(answer, nameof(answer));
        var validatedBadPoints = Guard.AgainstOutOfRange(badPoints, nameof(badPoints), MinBadPoints, MaxBadPoints);

        Text = validatedText;
        Answer = validatedAnswer;
        BadPoints = validatedBadPoints;
        Category = category;
        CategoryId = category.Id;
        Source = QuestionSource.Manual;
    }
}

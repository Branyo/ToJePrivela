using ToJePrivela.Domain.Common;

namespace ToJePrivela.Domain.Entities;

public class Question
{
    public const int TextMinLength = 8;
    public const int TextMaxLength = 512;
    public const int CategoryMinLength = 2;
    public const int CategoryMaxLength = 32;
    public const int MinDifficulty = 1;
    public const int MaxDifficulty = 5;

    private Question()
    {
        Text = string.Empty;
        Answer = string.Empty;
        Category = string.Empty;
    }

    public Question(string text, string answer, string category, int difficulty)
    {
        Text = Guard.AgainstInvalidLength(text, nameof(text), TextMinLength, TextMaxLength);
        Answer = Guard.AgainstNonNumeric(answer, nameof(answer));
        Category = Guard.AgainstInvalidLength(category, nameof(category), CategoryMinLength, CategoryMaxLength);
        Difficulty = Guard.AgainstOutOfRange(difficulty, nameof(difficulty), MinDifficulty, MaxDifficulty);
    }

    public int Id { get; private set; }

    public string Text { get; private set; }

    /// <summary>Stored as text, but always a numeric value.</summary>
    public string Answer { get; private set; }

    public string Category { get; private set; }

    public int Difficulty { get; private set; }

    /// <summary>Penalty for a wrong answer: the easier the question, the more it costs.</summary>
    public int BadPoints => MaxDifficulty + 1 - Difficulty;

    /// <summary>Validates everything before assigning, so a rejected update leaves the question untouched.</summary>
    public void Update(string text, string answer, string category, int difficulty)
    {
        var validatedText = Guard.AgainstInvalidLength(text, nameof(text), TextMinLength, TextMaxLength);
        var validatedAnswer = Guard.AgainstNonNumeric(answer, nameof(answer));
        var validatedCategory = Guard.AgainstInvalidLength(category, nameof(category), CategoryMinLength, CategoryMaxLength);
        var validatedDifficulty = Guard.AgainstOutOfRange(difficulty, nameof(difficulty), MinDifficulty, MaxDifficulty);

        Text = validatedText;
        Answer = validatedAnswer;
        Category = validatedCategory;
        Difficulty = validatedDifficulty;
    }
}

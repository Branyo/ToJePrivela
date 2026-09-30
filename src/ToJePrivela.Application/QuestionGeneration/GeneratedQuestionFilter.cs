using System.Globalization;
using System.Text.RegularExpressions;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionGeneration;

/// <summary>
/// Decides which generated questions are usable, whatever provider wrote them: the domain must accept
/// them, the answer must be readable in a quiz and the question must not give its answer away.
/// </summary>
public static partial class GeneratedQuestionFilter
{
    /// <summary>Anything larger is unreadable in a quiz; the prompt asks for "how many millions/billions" instead.</summary>
    public const decimal MaxAnswer = 1_000_000_000_000m;

    public static bool IsUsable(GeneratedQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        if (!Question.IsValidText(question.Text) || !Question.IsValidAnswer(question.Answer))
        {
            return false;
        }

        var value = Math.Abs(decimal.Parse(question.Answer, NumberStyles.Float, CultureInfo.InvariantCulture));

        return value <= MaxAnswer && !StatesNumber(question.Text, value);
    }

    /// <summary>A question that states its own answer gives it away ("In 1969, which year did Apollo 11 land?").</summary>
    private static bool StatesNumber(string text, decimal value) =>
        NumberInText().Matches(text).Any(match => decimal.TryParse(
            string.Concat(match.Value.Where(character => !char.IsWhiteSpace(character))).Replace(',', '.'),
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var number) && number == value);

    /// <summary>Plain numbers, decimals with either separator and space-grouped thousands ("1 000 000").</summary>
    [GeneratedRegex(@"\d{1,3}(?:\s\d{3})+|\d+(?:[.,]\d+)?")]
    private static partial Regex NumberInText();
}

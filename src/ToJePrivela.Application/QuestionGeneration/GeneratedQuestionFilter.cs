using System.Globalization;
using System.Text.RegularExpressions;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionGeneration;

/// <summary>
/// Decides which generated questions are usable, whatever provider wrote them: the domain must accept
/// both texts, the answer must be readable in a quiz and neither text may give the answer away.
/// </summary>
public static partial class GeneratedQuestionFilter
{
    /// <summary>Anything larger is unreadable in a quiz; the prompt asks for "how many millions/billions" instead.</summary>
    public const decimal MaxAnswer = 1_000_000_000_000m;

    public static bool IsUsable(GeneratedQuestion question) => Check(question) is null;

    /// <returns>Why the question is not usable; null when it is.</returns>
    public static QuestionRejection? Check(GeneratedQuestion question)
    {
        ArgumentNullException.ThrowIfNull(question);

        if (!Question.IsValidText(question.TextSk) || !Question.IsValidText(question.TextEn))
        {
            return QuestionRejection.InvalidText;
        }

        if (!Question.IsValidAnswer(question.Answer))
        {
            return QuestionRejection.InvalidAnswer;
        }

        var value = Math.Abs(decimal.Parse(question.Answer, NumberStyles.Float, CultureInfo.InvariantCulture));

        if (value > MaxAnswer)
        {
            return QuestionRejection.AnswerTooLarge;
        }

        return StatesNumber(question.TextSk, value) || StatesNumber(question.TextEn, value)
            ? QuestionRejection.StatesAnswer
            : null;
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

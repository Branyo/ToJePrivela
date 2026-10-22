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
        NumberInText().Matches(text).Any(match => Readings(match.Value).Contains(value));

    /// <summary>
    /// Every value a number in the text may stand for, since "1,000" is a thousand in English but one in Slovak: all its
    /// separators as thousands ones, and its last separator as the decimal point when it is the only one of its kind
    /// ("3,5", "1,000.5", but not "1,000,000"). Leaving out a good question costs less than keeping a giveaway.
    /// </summary>
    private static IEnumerable<decimal> Readings(string number)
    {
        var digits = string.Concat(number.Where(character => !char.IsWhiteSpace(character)));
        var last = digits.LastIndexOfAny(Separators);

        if (Parse(WithoutSeparators(digits)) is { } grouped)
        {
            yield return grouped;
        }

        if (last >= 0
            && digits.IndexOf(digits[last], StringComparison.Ordinal) == last
            && Parse($"{WithoutSeparators(digits[..last])}.{digits[(last + 1)..]}") is { } withDecimals)
        {
            yield return withDecimals;
        }
    }

    private static readonly char[] Separators = [',', '.'];

    private static string WithoutSeparators(string digits) => string.Concat(digits.Where(character => !Separators.Contains(character)));

    private static decimal? Parse(string digits) =>
        decimal.TryParse(digits, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number) ? number : null;

    /// <summary>
    /// Plain numbers, decimals with either separator and thousands grouped by spaces, commas or dots ("1 000 000",
    /// "1,000,000", "1.000,5").
    /// </summary>
    [GeneratedRegex(@"\d{1,3}(?:[,.\s]\d{3})+(?:[.,]\d+)?|\d+(?:[.,]\d+)?")]
    private static partial Regex NumberInText();
}

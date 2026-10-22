using System.Text.Json;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Ai.Parsing;

/// <summary>Finds the array in the reply with <see cref="JsonArrayText"/>, past any prose or markdown fences around it.</summary>
public sealed class GeneratedQuestionParser : IGeneratedQuestionParser
{
    public IReadOnlyList<ParsedQuestion> Parse(string? reply) =>
        ReadArray(reply, JsonValueKind.Object, items => items
            .Select(ToParsedQuestion)
            .OfType<ParsedQuestion>()
            .ToList());

    public IReadOnlyList<string> ParseSubtopics(string? reply) =>
        ReadArray(reply, JsonValueKind.String, items => items
            .Where(element => element.ValueKind == JsonValueKind.String)
            .Select(element => element.GetString()!.Trim())
            .Where(subtopic => subtopic.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList());

    /// <summary>Anything without a readable JSON array yields an empty list.</summary>
    private static IReadOnlyList<TItem> ReadArray<TItem>(
        string? reply,
        JsonValueKind itemKind,
        Func<IEnumerable<JsonElement>, IReadOnlyList<TItem>> readItems)
    {
        using var document = JsonArrayText.Find(reply, itemKind);

        return document is null ? [] : readItems(document.RootElement.EnumerateArray());
    }

    private static ParsedQuestion? ToParsedQuestion(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var questionSk = JsonArrayText.ReadScalar(element, "questionSk");
        var questionEn = JsonArrayText.ReadScalar(element, "questionEn");
        var answer = JsonArrayText.ReadScalar(element, "answer");

        // A question without both of its texts cannot be stored, so it counts as malformed.
        return string.IsNullOrWhiteSpace(questionSk) || string.IsNullOrWhiteSpace(questionEn) || string.IsNullOrWhiteSpace(answer)
            ? null
            : new ParsedQuestion(questionSk, questionEn, answer);
    }
}

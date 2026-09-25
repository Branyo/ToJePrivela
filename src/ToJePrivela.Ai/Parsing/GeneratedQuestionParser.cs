using System.Text.Json;

namespace ToJePrivela.Ai.Parsing;

public sealed class GeneratedQuestionParser : IGeneratedQuestionParser
{
    public IReadOnlyList<ParsedQuestion> Parse(string? reply) =>
        ReadArray(reply, items => items
            .Select(ToParsedQuestion)
            .OfType<ParsedQuestion>()
            .ToList());

    public IReadOnlyList<string> ParseSubtopics(string? reply) =>
        ReadArray(reply, items => items
            .Where(element => element.ValueKind == JsonValueKind.String)
            .Select(element => element.GetString()!.Trim())
            .Where(subtopic => subtopic.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList());

    /// <summary>Anything that is not a readable JSON array yields an empty list.</summary>
    private static IReadOnlyList<TItem> ReadArray<TItem>(
        string? reply,
        Func<IEnumerable<JsonElement>, IReadOnlyList<TItem>> readItems)
    {
        var json = ExtractJsonArray(reply);

        if (json is null)
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            return document.RootElement.ValueKind == JsonValueKind.Array
                ? readItems(document.RootElement.EnumerateArray())
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static ParsedQuestion? ToParsedQuestion(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var question = ReadString(element, "question");
        var answer = ReadString(element, "answer");

        return string.IsNullOrWhiteSpace(question) || string.IsNullOrWhiteSpace(answer)
            ? null
            : new ParsedQuestion(question, answer);
    }

    /// <summary>The model answers with either a JSON number or a quoted number.</summary>
    private static string? ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
            _ => null
        };
    }

    /// <summary>Trims prose or markdown fences that models like to add around the array.</summary>
    private static string? ExtractJsonArray(string? reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            return null;
        }

        var start = reply.IndexOf('[', StringComparison.Ordinal);
        var end = reply.LastIndexOf(']');

        return start >= 0 && end > start
            ? reply[start..(end + 1)]
            : null;
    }
}

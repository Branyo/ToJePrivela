using System.Text.Json;
using ToJePrivela.Application.Abstractions.Ai;

namespace ToJePrivela.Application.QuestionImport;

/// <summary>
/// Reads questions in the format the AI generator answers with: a JSON array of
/// <c>{"questionSk": "…", "questionEn": "…", "answer": 42}</c> objects, the answer a JSON number or a quoted one.
/// Prose or markdown fences around the array are ignored, so a model's reply can be pasted as it is.
/// </summary>
public static class QuestionImportParser
{
    /// <returns>One entry per array item in its order, null for an item that is not a question; null for the whole
    /// text when it holds no readable JSON array.</returns>
    public static IReadOnlyList<GeneratedQuestion?>? Parse(string? json)
    {
        var array = ExtractJsonArray(json);

        if (array is null)
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(array);

            return document.RootElement.EnumerateArray().Select(ToQuestion).ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static GeneratedQuestion? ToQuestion(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var questionSk = ReadString(element, "questionSk");
        var questionEn = ReadString(element, "questionEn");
        var answer = ReadString(element, "answer");

        return string.IsNullOrWhiteSpace(questionSk) || string.IsNullOrWhiteSpace(questionEn) || string.IsNullOrWhiteSpace(answer)
            ? null
            : new GeneratedQuestion(questionSk.Trim(), questionEn.Trim(), answer.Trim());
    }

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
            _ => null
        };
    }

    private static string? ExtractJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var start = json.IndexOf('[', StringComparison.Ordinal);
        var end = json.LastIndexOf(']');

        return start >= 0 && end > start
            ? json[start..(end + 1)]
            : null;
    }
}

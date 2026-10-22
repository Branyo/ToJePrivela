using System.Text.Json;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.QuestionImport;

/// <summary>
/// Reads questions in the format the AI generator answers with: a JSON array of
/// <c>{"questionSk": "…", "questionEn": "…", "answer": 42}</c> objects, the answer a JSON number or a quoted one.
/// Prose or markdown fences around the array are ignored (<see cref="JsonArrayText"/>), so a model's reply can be pasted as it is.
/// </summary>
public static class QuestionImportParser
{
    /// <returns>One entry per array item in its order, null for an item that is not a question; null for the whole
    /// text when it holds no readable JSON array.</returns>
    public static IReadOnlyList<GeneratedQuestion?>? Parse(string? json)
    {
        using var document = JsonArrayText.Find(json, JsonValueKind.Object);

        return document?.RootElement.EnumerateArray().Select(ToQuestion).ToList();
    }

    private static GeneratedQuestion? ToQuestion(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var questionSk = JsonArrayText.ReadScalar(element, "questionSk");
        var questionEn = JsonArrayText.ReadScalar(element, "questionEn");
        var answer = JsonArrayText.ReadScalar(element, "answer");

        return string.IsNullOrWhiteSpace(questionSk) || string.IsNullOrWhiteSpace(questionEn) || string.IsNullOrWhiteSpace(answer)
            ? null
            : new GeneratedQuestion(questionSk.Trim(), questionEn.Trim(), answer.Trim());
    }
}

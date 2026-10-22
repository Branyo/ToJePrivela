using System.Text;
using System.Text.Json;

namespace ToJePrivela.Application.Common;

/// <summary>
/// Finds the JSON array in a text that may carry prose or markdown fences around it, as a model's reply (or such a reply
/// pasted for an import) does. Shared by the AI reply parser and the question import, so both find the array alike.
/// </summary>
public static class JsonArrayText
{
    /// <summary>
    /// How many <c>[</c> are tried as the array's start; prose before the array rarely holds more, and a text full of
    /// unbalanced brackets must not be parsed again from each of them.
    /// </summary>
    public const int MaxStarts = 64;

    /// <summary>
    /// The first array in <paramref name="text"/> holding an item of <paramref name="itemKind"/>, so a bracket in the
    /// prose ("Here are [20] questions: [...]") is passed over; else the first readable array, empty or not; null when
    /// there is none. Arrays nested in one already read are not tried again.
    /// </summary>
    public static JsonDocument? Find(string? text, JsonValueKind itemKind)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var utf8 = Encoding.UTF8.GetBytes(text);
        JsonDocument? firstArray = null;
        var start = Array.IndexOf(utf8, (byte)'[');

        for (var tries = 0; start >= 0 && tries < MaxStarts; tries++)
        {
            var next = start + 1;

            if (TryReadValue(utf8, start, out var document, out var length))
            {
                if (document.RootElement.EnumerateArray().Any(item => item.ValueKind == itemKind))
                {
                    firstArray?.Dispose();
                    return document;
                }

                if (firstArray is null)
                {
                    firstArray = document;
                }
                else
                {
                    document.Dispose();
                }

                next = start + length;
            }

            start = next < utf8.Length ? Array.IndexOf(utf8, (byte)'[', next) : -1;
        }

        return firstArray;
    }

    /// <summary>A property given as a string or a number (models write answers either way), as text; null otherwise.</summary>
    public static string? ReadScalar(JsonElement element, string propertyName)
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

    /// <summary>Reads the one value starting at <paramref name="start"/>; whatever text follows it is ignored.</summary>
    private static bool TryReadValue(byte[] utf8, int start, out JsonDocument document, out int length)
    {
        var reader = new Utf8JsonReader(utf8.AsSpan(start));

        try
        {
            document = JsonDocument.ParseValue(ref reader);
            length = (int)reader.BytesConsumed;
            return true;
        }
        catch (JsonException)
        {
            document = null!;
            length = 0;
            return false;
        }
    }
}

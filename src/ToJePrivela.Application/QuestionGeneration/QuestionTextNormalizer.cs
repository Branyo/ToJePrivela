using System.Text;

namespace ToJePrivela.Application.QuestionGeneration;

/// <summary>
/// Canonical form used to spot duplicates: case, surrounding and repeated whitespace and trailing
/// punctuation do not matter; diacritics do ("Koľko" and "Kolko" are different questions).
/// </summary>
public static class QuestionTextNormalizer
{
    private static readonly char[] TrailingPunctuation = ['?', '.', '!', '…'];

    public static string Normalize(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;

        foreach (var character in text.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = true;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString().TrimEnd(TrailingPunctuation).TrimEnd();
    }
}

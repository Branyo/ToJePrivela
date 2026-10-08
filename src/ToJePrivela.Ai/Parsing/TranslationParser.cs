using System.Text.RegularExpressions;

namespace ToJePrivela.Ai.Parsing;

public sealed partial class TranslationParser : ITranslationParser
{
    [GeneratedRegex(@"^(translation|translated name|preklad)\s*:\s*", RegexOptions.IgnoreCase)]
    private static partial Regex LabelPrefix();

    private static readonly char[] Wrapping = ['"', '\'', '`', '„', '“', '”', '‚', '‘', '’', '*'];
    private static readonly char[] TrailingWrapping = [.. Wrapping, '.'];

    /// <summary>
    /// Takes the first non-blank line and strips the label ("Translation:"), quotes, markdown and full stop models like
    /// to add.
    /// </summary>
    public string? Parse(string? reply)
    {
        var line = reply?
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        var text = line is null
            ? null
            : LabelPrefix().Replace(line.TrimStart(Wrapping), string.Empty).TrimStart(Wrapping).TrimEnd(TrailingWrapping).Trim();

        return string.IsNullOrEmpty(text) ? null : text;
    }
}

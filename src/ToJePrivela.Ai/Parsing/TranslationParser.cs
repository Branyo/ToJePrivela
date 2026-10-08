namespace ToJePrivela.Ai.Parsing;

public sealed class TranslationParser : ITranslationParser
{
    private static readonly char[] Wrapping = ['"', '\'', '`', '„', '“', '”', '‚', '‘', '’', '*'];
    private static readonly char[] TrailingWrapping = [.. Wrapping, '.'];

    /// <summary>Takes the first non-blank line and strips the quotes, markdown and full stop models like to add.</summary>
    public string? Parse(string? reply)
    {
        var line = reply?
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        var text = line?.TrimStart(Wrapping).TrimEnd(TrailingWrapping).Trim();

        return string.IsNullOrEmpty(text) ? null : text;
    }
}

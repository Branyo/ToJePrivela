using System.Globalization;
using System.Text;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Ai.Prompts;

/// <summary>A deliberately simple prompt: the texts it translates are short names, such as quiz category names.</summary>
public sealed class TranslationPromptBuilder : ITranslationPromptBuilder
{
    public string Build(string text, Language from, Language to)
    {
        ArgumentNullException.ThrowIfNull(text);

        var builder = new StringBuilder();

        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Translate this quiz category name from {NameOf(from)} to {NameOf(to)}: {text.Trim()}");
        builder.AppendLine("Keep it short and natural, the way a quiz would name the category.");
        builder.AppendLine("Answer with the translated name only, without quotes, punctuation or commentary.");

        return builder.ToString();
    }

    public static string NameOf(Language language) => language switch
    {
        Language.Sk => "Slovak",
        Language.En => "English",
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported language.")
    };
}

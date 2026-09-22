using System.Globalization;
using System.Text;

namespace ToJePrivela.Ai.Prompts;

public sealed class QuestionPromptBuilder : IQuestionPromptBuilder
{
    public string Build(string category, int count, string language)
    {
        var builder = new StringBuilder();

        builder.AppendLine("You are generating ORIGINAL quiz questions whose answer is always a numeric value.");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Category: {category}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Target language: {language}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Question count: {count}");
        builder.AppendLine("Rules:");
        builder.AppendLine("- Answer with a bare JSON array only, without markdown fences or commentary.");
        builder.AppendLine("- Each item has exactly the keys \"question\" and \"answer\".");
        builder.AppendLine("- The answer must be a plain number, without units or extra text.");
        builder.AppendLine("- Avoid duplicates and trivial rephrasings.");
        builder.AppendLine("Example:");
        builder.AppendLine("[{\"question\": \"Which year was ChatGPT publicly released?\", \"answer\": 2022}]");

        return builder.ToString();
    }
}

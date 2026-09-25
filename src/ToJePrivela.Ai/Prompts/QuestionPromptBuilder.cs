using System.Globalization;
using System.Text;
using ToJePrivela.Application.Abstractions.Ai;

namespace ToJePrivela.Ai.Prompts;

public sealed class QuestionPromptBuilder : IQuestionPromptBuilder
{
    public string BuildQuestions(QuestionGenerationRequest request, string language)
    {
        ArgumentNullException.ThrowIfNull(request);

        var builder = new StringBuilder();

        builder.AppendLine("You are generating ORIGINAL quiz questions whose answer is always a numeric value.");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Category: {request.Category}");

        if (!string.IsNullOrWhiteSpace(request.Subtopic))
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- Subtopic: {request.Subtopic} (stay within it)");
        }

        builder.AppendLine(CultureInfo.InvariantCulture, $"- Target language: {language}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Question count: {request.Count}");
        builder.AppendLine("Rules:");
        builder.AppendLine("- Answer with a bare JSON array only, without markdown fences or commentary.");
        builder.AppendLine("- Each item has exactly the keys \"question\" and \"answer\".");
        builder.AppendLine("- The answer must be a plain integer number, without units or extra text.");
        builder.AppendLine("- Define questions unambiguously (with units that answer expresses if it is necessary).");
        builder.AppendLine("- Avoid duplicates and trivial rephrasings.");
        builder.AppendLine("- Use only well-documented facts you are certain of; every answer must be verifiable in an encyclopedia.");
        builder.AppendLine("- Each question must have exactly one correct answer that sources agree on; skip disputed or estimated values.");
        builder.AppendLine("- For values that change over time (population, records, prices), name the year the answer refers to.");
        builder.AppendLine("- Never invent people, events or numbers. If you are unsure of a fact, leave the question out: fewer questions are better than a wrong answer.");

        if (request.ExcludedQuestions is { Count: > 0 } excluded)
        {
            builder.AppendLine("- These questions already exist; do not repeat or rephrase any of them:");

            foreach (var question in excluded)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  * {question}");
            }
        }

        builder.AppendLine("Example:");
        builder.AppendLine("[{\"question\": \"Which year was ChatGPT publicly released?\", \"answer\": 2022}]");

        return builder.ToString();
    }

    public string BuildSubtopics(string category, int count, string language)
    {
        var builder = new StringBuilder();

        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Split the quiz category \"{category}\" into {count} distinct, non-overlapping subtopics.");
        builder.AppendLine("Each subtopic must offer many facts that can be asked about with a numeric answer.");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Target language: {language}");
        builder.AppendLine("- Answer with a bare JSON array of strings only, without markdown fences or commentary.");
        builder.AppendLine("Example:");
        builder.AppendLine("[\"Formula 1 history\", \"Car engines\"]");

        return builder.ToString();
    }
}

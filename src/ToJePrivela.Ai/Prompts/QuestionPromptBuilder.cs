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
        builder.AppendLine("- Vary the kind of number asked for across the set; do not make most questions about years.");
        builder.AppendLine("  Mix e.g. a year, a number of years, how many (people, pieces, times, ...), a length in meters or kilometers,");
        builder.AppendLine("  a height, a weight in kilograms or tonnes, a speed, a temperature, an area, a price, a duration, a percentage.");
        builder.AppendLine("- Never reveal the answer in the question: the answer number (or a trivial calculation leading to it) must not appear in the question text.");
        builder.AppendLine("- The answer must not be larger than 1 000 000 000 000. For huge quantities ask in a bigger unit instead,");
        builder.AppendLine("  e.g. \"How many millions of inhabitants ...\" or \"How many billions of dollars ...\", and round the answer to that unit.");
        builder.AppendLine("- Use only well-documented facts you are certain of; every answer must be verifiable in an encyclopedia.");
        builder.AppendLine("- If possible prefer interesting / fun questions.");
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
        builder.AppendLine("[{\"question\": \"Which year was ChatGPT publicly released?\", \"answer\": 2022},");
        builder.AppendLine(" {\"question\": \"How many meters tall is the Eiffel Tower including its antennas (as of 2022)?\", \"answer\": 330},");
        builder.AppendLine(" {\"question\": \"How many millions of inhabitants did Japan have in 2020?\", \"answer\": 126}]");

        return builder.ToString();
    }

    public string BuildSubtopics(string category, int count, string language)
    {
        var builder = new StringBuilder();

        builder.AppendLine(CultureInfo.InvariantCulture,
            $"Split the quiz category \"{category}\" into {count} distinct, non-overlapping subtopics.");
        builder.AppendLine("Each subtopic must offer many facts that can be asked about with a numeric answer.");
        builder.AppendLine("- Where the category allows it, prefer broad, general subtopics over specific brands, titles or names");
        builder.AppendLine("  (for the category \"Toys\" choose \"Construction sets\" rather than \"Lego\").");
        builder.AppendLine("- The subtopics must not overlap: no subtopic may be a part, an example or a rephrasing of another one.");
        builder.AppendLine(CultureInfo.InvariantCulture, $"- Target language: {language}");
        builder.AppendLine("- Answer with a bare JSON array of strings only, without markdown fences or commentary.");
        builder.AppendLine("Example:");
        builder.AppendLine("[\"Formula 1 history\", \"Car engines\"]");

        return builder.ToString();
    }
}

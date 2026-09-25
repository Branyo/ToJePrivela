namespace ToJePrivela.Ai.Parsing;

public interface IGeneratedQuestionParser
{
    /// <summary>Reads the model reply; items that are malformed are skipped.</summary>
    IReadOnlyList<ParsedQuestion> Parse(string? reply);

    /// <summary>Reads a JSON array of strings; blank and non-string items are skipped.</summary>
    IReadOnlyList<string> ParseSubtopics(string? reply);
}

public sealed record ParsedQuestion(string Question, string Answer);

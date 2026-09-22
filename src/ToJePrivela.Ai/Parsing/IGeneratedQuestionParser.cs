namespace ToJePrivela.Ai.Parsing;

public interface IGeneratedQuestionParser
{
    /// <summary>Reads the model reply; items that are malformed are skipped.</summary>
    IReadOnlyList<ParsedQuestion> Parse(string? reply);
}

public sealed record ParsedQuestion(string Question, string Answer);

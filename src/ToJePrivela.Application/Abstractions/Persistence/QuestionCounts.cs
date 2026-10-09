namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>How many questions a category holds, and how many of them came from the AI.</summary>
public sealed record QuestionCounts(int Total, int Ai)
{
    public static readonly QuestionCounts None = new(0, 0);
}

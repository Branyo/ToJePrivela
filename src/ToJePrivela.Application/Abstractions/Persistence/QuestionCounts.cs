using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Abstractions.Persistence;

/// <summary>How many questions a category holds, and how many of them came from the AI.</summary>
public sealed record QuestionCounts(int Total, int Ai)
{
    public static readonly QuestionCounts None = new(0, 0);
}

/// <summary>A category with its question counts, read together so the counts match the category.</summary>
public sealed record CountedCategory(QuestionCategory Category, QuestionCounts Counts);

using ToJePrivela.Application.Questions;

namespace ToJePrivela.Application.Tests.Common;

/// <summary>Always takes the last candidate and remembers what it was offered.</summary>
public sealed class FixedQuestionPicker : IQuestionPicker
{
    public IReadOnlyList<int> Offered { get; private set; } = [];

    public int Pick(IReadOnlyList<int> candidateIds)
    {
        Offered = candidateIds;
        return candidateIds[^1];
    }
}

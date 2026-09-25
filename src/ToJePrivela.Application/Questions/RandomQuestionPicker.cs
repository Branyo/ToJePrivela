namespace ToJePrivela.Application.Questions;

public sealed class RandomQuestionPicker : IQuestionPicker
{
    public int Pick(IReadOnlyList<int> candidateIds) => candidateIds[Random.Shared.Next(candidateIds.Count)];
}

namespace ToJePrivela.Application.Questions.Dtos;

public sealed class RandomQuestionFilter
{
    /// <summary>Categories to choose from; every category when left empty.</summary>
    public int[] CategoryIds { get; init; } = [];
}

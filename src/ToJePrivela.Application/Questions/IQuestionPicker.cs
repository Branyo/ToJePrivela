namespace ToJePrivela.Application.Questions;

/// <summary>Chooses one of the equally eligible questions; a port so tests stay deterministic.</summary>
public interface IQuestionPicker
{
    /// <param name="candidateIds">Never empty.</param>
    int Pick(IReadOnlyList<int> candidateIds);
}

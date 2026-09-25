namespace ToJePrivela.Application.Questions;

/// <summary>Chooses bad points for a question nobody set them for; a port so tests stay deterministic.</summary>
public interface IBadPointsPicker
{
    int Pick();
}

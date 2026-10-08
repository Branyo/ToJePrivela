namespace ToJePrivela.Ai.Parsing;

public interface ITranslationParser
{
    /// <summary>Reads the translated text from the model reply; null when there is none.</summary>
    string? Parse(string? reply);
}

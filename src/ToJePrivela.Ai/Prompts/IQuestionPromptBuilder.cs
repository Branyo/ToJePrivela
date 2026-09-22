namespace ToJePrivela.Ai.Prompts;

public interface IQuestionPromptBuilder
{
    string Build(string category, int count, string language);
}

using ToJePrivela.Domain.Common;

namespace ToJePrivela.Ai.Prompts;

public interface ITranslationPromptBuilder
{
    string Build(string text, Language from, Language to);
}

using ToJePrivela.Application.Abstractions.Ai;

namespace ToJePrivela.Ai.Prompts;

public interface IQuestionPromptBuilder
{
    /// <summary>Asks for every question in Slovak and in English.</summary>
    string BuildQuestions(QuestionGenerationRequest request);

    /// <summary>Asks for the subtopics in English, like the category name it is given.</summary>
    string BuildSubtopics(string category, int count);
}

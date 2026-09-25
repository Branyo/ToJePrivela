using ToJePrivela.Application.Abstractions.Ai;

namespace ToJePrivela.Ai.Prompts;

public interface IQuestionPromptBuilder
{
    string BuildQuestions(QuestionGenerationRequest request, string language);

    string BuildSubtopics(string category, int count, string language);
}

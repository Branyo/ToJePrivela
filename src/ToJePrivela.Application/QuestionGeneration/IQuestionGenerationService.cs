using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionGeneration;

/// <summary>
/// Produces AI questions for a category without storing them, so each calling use case decides
/// what to save together with them.
/// </summary>
public interface IQuestionGenerationService
{
    Task<QuestionGenerationResult> GenerateAsync(
        QuestionCategory category,
        int count,
        CancellationToken cancellationToken = default);
}

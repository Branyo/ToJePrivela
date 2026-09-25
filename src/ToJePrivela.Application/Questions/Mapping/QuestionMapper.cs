using ToJePrivela.Application.Questions.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Questions.Mapping;

public static class QuestionMapper
{
    /// <summary>The category name is empty when the category was not loaded.</summary>
    public static QuestionDto ToDto(Question question) => new(
        question.Id,
        question.Text,
        question.Answer,
        question.CategoryId,
        question.Category?.Name ?? string.Empty,
        question.BadPoints,
        question.Source.ToString(),
        question.CreatedAt,
        question.ViewCount,
        question.LastViewedAt);

    public static IReadOnlyList<QuestionDto> ToDtos(IEnumerable<Question> questions) =>
        questions.Select(ToDto).ToList();

    public static Question ToEntity(CreateQuestionRequest request, QuestionCategory category, int badPoints, DateTime createdAt) =>
        new(request.Text, request.Answer, category, badPoints, QuestionSource.Manual, createdAt);
}

using ToJePrivela.Application.Questions.Dtos;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Questions.Mapping;

public static class QuestionMapper
{
    /// <summary>
    /// Text and category name come in <paramref name="language"/>; the category name is empty when the category was
    /// not loaded.
    /// </summary>
    public static QuestionDto ToDto(Question question, Language language) => new(
        question.Id,
        question.TextIn(language),
        question.TextSk,
        question.TextEn,
        question.Answer,
        question.CategoryId,
        question.Category?.NameIn(language) ?? string.Empty,
        question.BadPoints,
        question.Source.ToString(),
        question.CreatedAt,
        question.ViewCount,
        question.LastViewedAt);

    public static IReadOnlyList<QuestionDto> ToDtos(IEnumerable<Question> questions, Language language) =>
        questions.Select(question => ToDto(question, language)).ToList();

    public static Question ToEntity(CreateQuestionRequest request, QuestionCategory category, int badPoints, DateTime createdAt) =>
        new(request.TextSk, request.TextEn, request.Answer, category, badPoints, QuestionSource.Manual, createdAt);
}

using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.QuestionGeneration.Mapping;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionCategories.Mapping;

public static class QuestionCategoryMapper
{
    public static QuestionCategoryDto ToDto(QuestionCategory category) => new(category.Id, category.Name);

    public static IReadOnlyList<QuestionCategoryDto> ToDtos(IEnumerable<QuestionCategory> categories) =>
        categories.Select(ToDto).ToList();

    public static CreatedQuestionCategoryDto ToCreatedDto(QuestionCategory category, QuestionGenerationResult generation) => new(
        category.Id,
        category.Name,
        QuestionGenerationMapper.ToSummaryDto(generation));

    public static QuestionCategory ToEntity(CreateQuestionCategoryRequest request) =>
        new(request.Name);
}

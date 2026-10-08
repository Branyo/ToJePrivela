using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.QuestionGeneration.Mapping;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionCategories.Mapping;

public static class QuestionCategoryMapper
{
    public static QuestionCategoryDto ToDto(QuestionCategory category, Language language) =>
        new(category.Id, category.NameIn(language), category.NameSk, category.NameEn);

    public static IReadOnlyList<QuestionCategoryDto> ToDtos(IEnumerable<QuestionCategory> categories, Language language) =>
        categories.Select(category => ToDto(category, language)).ToList();

    public static CreatedQuestionCategoryDto ToCreatedDto(
        QuestionCategory category,
        QuestionGenerationResult generation,
        Language language) => new(
        category.Id,
        category.NameIn(language),
        category.NameSk,
        category.NameEn,
        QuestionGenerationMapper.ToSummaryDto(generation));
}

using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.QuestionGeneration.Mapping;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionCategories.Mapping;

public static class QuestionCategoryMapper
{
    public static QuestionCategoryDto ToDto(QuestionCategory category, QuestionCounts counts, Language language) =>
        new(category.Id, category.NameIn(language), category.NameSk, category.NameEn, counts.Total, counts.Ai);

    /// <param name="counts">Per category id; a category that is missing has no questions.</param>
    public static IReadOnlyList<QuestionCategoryDto> ToDtos(
        IEnumerable<QuestionCategory> categories,
        IReadOnlyDictionary<int, QuestionCounts> counts,
        Language language) =>
        categories.Select(category => ToDto(category, counts.GetValueOrDefault(category.Id, QuestionCounts.None), language)).ToList();

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

using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.QuestionGeneration.Mapping;
using ToJePrivela.Application.QuestionImport;
using ToJePrivela.Application.QuestionImport.Mapping;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionCategories.Mapping;

public static class QuestionCategoryMapper
{
    public static QuestionCategoryDto ToDto(QuestionCategory category, QuestionCounts counts, Language language) =>
        new(category.Id, category.NameIn(language), category.NameSk, category.NameEn, counts.Total, counts.Ai);

    public static QuestionCategoryDto ToDto(CountedCategory counted, Language language) =>
        ToDto(counted.Category, counted.Counts, language);

    public static IReadOnlyList<QuestionCategoryDto> ToDtos(IEnumerable<CountedCategory> categories, Language language) =>
        categories.Select(counted => ToDto(counted, language)).ToList();

    public static CreatedQuestionCategoryDto ToCreatedDto(
        QuestionCategory category,
        QuestionGenerationResult generation,
        QuestionImportOutcome? import,
        Language language)
    {
        var questionCount = generation.Questions.Count + (import?.Questions.Count ?? 0);

        return new(
            category.Id,
            category.NameIn(language),
            category.NameSk,
            category.NameEn,
            questionCount,
            questionCount,
            QuestionGenerationMapper.ToSummaryDto(generation),
            import is null ? null : QuestionImportMapper.ToSummaryDto(import));
    }
}

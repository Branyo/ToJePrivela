using ToJePrivela.Application.Players.Mapping;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionCategories.Mapping;

public static class QuestionCategoryMapper
{
    public static QuestionCategoryDto ToDto(QuestionCategory category) => new(
        category.Id,
        category.Name,
        category.AddedByPlayer is null ? null : PlayerMapper.ToDto(category.AddedByPlayer));

    public static IReadOnlyList<QuestionCategoryDto> ToDtos(IEnumerable<QuestionCategory> categories) =>
        categories.Select(ToDto).ToList();

    public static QuestionCategory ToEntity(CreateQuestionCategoryRequest request) =>
        new(request.Name, request.AddedByPlayerId);
}

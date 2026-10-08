namespace ToJePrivela.Application.QuestionCategories.Dtos;

/// <param name="Name">The name in the request's language, ready to show.</param>
public sealed record QuestionCategoryDto(int Id, string Name, string NameSk, string NameEn);

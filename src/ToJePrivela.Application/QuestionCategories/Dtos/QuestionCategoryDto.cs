namespace ToJePrivela.Application.QuestionCategories.Dtos;

/// <param name="Name">The name in the request's language, ready to show.</param>
/// <param name="QuestionCount">Every question in the category.</param>
/// <param name="AiQuestionCount">Those of them the AI wrote (an edited one counts as manual).</param>
public sealed record QuestionCategoryDto(int Id, string Name, string NameSk, string NameEn, int QuestionCount, int AiQuestionCount);

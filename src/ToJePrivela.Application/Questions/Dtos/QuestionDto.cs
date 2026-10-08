namespace ToJePrivela.Application.Questions.Dtos;

/// <param name="CategoryName">The category's name in the request's language, ready to show.</param>
public sealed record QuestionDto(
    int Id,
    string Text,
    string Answer,
    int CategoryId,
    string CategoryName,
    int BadPoints,
    string Source,
    DateTime CreatedAt,
    int ViewCount,
    DateTime? LastViewedAt);

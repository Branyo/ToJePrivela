namespace ToJePrivela.Application.Questions.Dtos;

/// <param name="Text">The text in the request's language, ready to show (Slovak while an English one is missing).</param>
/// <param name="TextEn">Null for a question stored before texts became bilingual.</param>
/// <param name="CategoryName">The category's name in the request's language, ready to show.</param>
public sealed record QuestionDto(
    int Id,
    string Text,
    string TextSk,
    string? TextEn,
    string Answer,
    int CategoryId,
    string CategoryName,
    int BadPoints,
    string Source,
    DateTime CreatedAt,
    int ViewCount,
    DateTime? LastViewedAt);

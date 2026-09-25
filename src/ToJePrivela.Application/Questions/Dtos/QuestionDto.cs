namespace ToJePrivela.Application.Questions.Dtos;

public sealed record QuestionDto(
    int Id,
    string Text,
    string Answer,
    int CategoryId,
    string CategoryName,
    int BadPoints,
    string Source,
    DateTime CreatedAt);

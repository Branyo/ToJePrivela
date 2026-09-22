namespace ToJePrivela.Application.Questions.Dtos;

public sealed record QuestionDto(int Id, string Text, string Answer, string Category, int Difficulty, int BadPoints);

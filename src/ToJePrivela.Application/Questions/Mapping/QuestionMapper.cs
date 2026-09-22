using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Questions.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Questions.Mapping;

public static class QuestionMapper
{
    public static QuestionDto ToDto(Question question) => new(
        question.Id,
        question.Text,
        question.Answer,
        question.Category,
        question.Difficulty,
        question.BadPoints);

    public static IReadOnlyList<QuestionDto> ToDtos(IEnumerable<Question> questions) =>
        questions.Select(ToDto).ToList();

    public static Question ToEntity(CreateQuestionRequest request) =>
        new(request.Text, request.Answer, request.Category, request.Difficulty);

    public static GeneratedQuestionDto ToDto(GeneratedQuestion question) => new(
        question.Text,
        question.Answer,
        question.Category,
        question.Difficulty,
        Question.MaxDifficulty + 1 - question.Difficulty);

    public static IReadOnlyList<GeneratedQuestionDto> ToDtos(IEnumerable<GeneratedQuestion> questions) =>
        questions.Select(ToDto).ToList();
}

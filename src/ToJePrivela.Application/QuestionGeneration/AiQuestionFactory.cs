using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Questions;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionGeneration;

/// <summary>Builds the stored form of a generated or imported question: an AI question with random bad points.</summary>
public static class AiQuestionFactory
{
    public static Question Create(
        GeneratedQuestion item,
        QuestionCategory category,
        IBadPointsPicker badPoints,
        DateTime createdAt) =>
        new(item.TextSk, item.TextEn, item.Answer, category, badPoints.Pick(), QuestionSource.Ai, createdAt);
}

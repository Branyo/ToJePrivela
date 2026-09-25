using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Questions;

public sealed class RandomBadPointsPicker : IBadPointsPicker
{
    public int Pick() => Random.Shared.Next(Question.MinBadPoints, Question.MaxBadPoints + 1);
}

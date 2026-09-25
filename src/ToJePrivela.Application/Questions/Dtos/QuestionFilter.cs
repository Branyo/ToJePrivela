using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Questions.Dtos;

public sealed class QuestionFilter
{
    [Range(1, int.MaxValue, ErrorMessage = "Category id should be positive.")]
    public int? CategoryId { get; init; }

    public QuestionSource? Source { get; init; }
}

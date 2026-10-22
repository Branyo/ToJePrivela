using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Questions.Dtos;

/// <summary>One category's questions at a time: the whole bank is never listed at once.</summary>
public sealed class QuestionFilter
{
    [Required(ErrorMessage = "Category id is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Category id should be positive.")]
    public int? CategoryId { get; init; }

    public QuestionSource? Source { get; init; }
}

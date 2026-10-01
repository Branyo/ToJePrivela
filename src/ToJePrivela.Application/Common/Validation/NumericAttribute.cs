using System.ComponentModel.DataAnnotations;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Common.Validation;

/// <summary>
/// A numeric answer as <see cref="Question.IsValidAnswer"/> accepts it: digits with an optional sign and decimal
/// point, never a thousands separator. Null passes; combine with <see cref="RequiredAttribute"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class NumericAttribute : ValidationAttribute
{
    public NumericAttribute()
        : base("{0} should be a number such as 42 or -3.5, without separators.")
    {
    }

    public override bool IsValid(object? value) => value is null || value is string text && Question.IsValidAnswer(text);
}

using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ToJePrivela.Application.Common.Validation;

/// <summary>
/// How many different items a collection holds, so <c>[1, 1]</c> counts as one. Null passes; combine with
/// <see cref="RequiredAttribute"/>. In <see cref="ValidationAttribute.ErrorMessage"/>, {0} is the field, {1} the
/// minimum and {2} the maximum.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class DistinctCountAttribute : ValidationAttribute
{
    public DistinctCountAttribute(int minimum, int maximum)
        : base("{0} should hold from {1} to {2} different items.")
    {
        Minimum = minimum;
        Maximum = maximum;
    }

    public int Minimum { get; }

    public int Maximum { get; }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is not IEnumerable items)
        {
            return false;
        }

        var count = items.Cast<object?>().Distinct().Count();
        return count >= Minimum && count <= Maximum;
    }

    public override string FormatErrorMessage(string name) =>
        string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, Minimum, Maximum);
}

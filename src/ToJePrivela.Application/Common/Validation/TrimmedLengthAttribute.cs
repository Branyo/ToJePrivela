using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace ToJePrivela.Application.Common.Validation;

/// <summary>
/// Length limits measured after trimming, the way the entities store text, so <c>"  a  "</c> is rejected here
/// rather than by the domain. Null passes; combine with <see cref="RequiredAttribute"/>.
/// In <see cref="ValidationAttribute.ErrorMessage"/>, {0} is the field, {1} the minimum and {2} the maximum.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class TrimmedLengthAttribute : ValidationAttribute
{
    public TrimmedLengthAttribute(int minimumLength, int maximumLength)
        : base("{0} should have from {1} to {2} characters.")
    {
        MinimumLength = minimumLength;
        MaximumLength = maximumLength;
    }

    public int MinimumLength { get; }

    public int MaximumLength { get; }

    public override bool IsValid(object? value) =>
        value is null || value is string text && text.Trim().Length >= MinimumLength && text.Trim().Length <= MaximumLength;

    public override string FormatErrorMessage(string name) =>
        string.Format(CultureInfo.CurrentCulture, ErrorMessageString, name, MinimumLength, MaximumLength);
}

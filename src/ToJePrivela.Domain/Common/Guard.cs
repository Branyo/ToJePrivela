using System.Globalization;

namespace ToJePrivela.Domain.Common;

/// <summary>Invariant checks shared by the entities.</summary>
public static class Guard
{
    public static string AgainstNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{parameterName} must not be empty.");
        }

        return value.Trim();
    }

    public static string AgainstInvalidLength(string? value, string parameterName, int minLength, int maxLength)
    {
        var trimmed = AgainstNullOrWhiteSpace(value, parameterName);

        if (trimmed.Length < minLength || trimmed.Length > maxLength)
        {
            throw new DomainException(
                $"{parameterName} must have between {minLength} and {maxLength} characters.");
        }

        return trimmed;
    }

    public static int AgainstOutOfRange(int value, string parameterName, int min, int max)
    {
        if (value < min || value > max)
        {
            throw new DomainException($"{parameterName} must be between {min} and {max}.");
        }

        return value;
    }

    public static int AgainstNegative(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new DomainException($"{parameterName} must not be negative.");
        }

        return value;
    }

    public static string AgainstNonNumeric(string? value, string parameterName)
    {
        var trimmed = AgainstNullOrWhiteSpace(value, parameterName);

        if (!IsNumeric(trimmed))
        {
            throw new DomainException($"{parameterName} must be a numeric value.");
        }

        return trimmed;
    }

    /// <summary>Separators are rejected: "3,5" must not silently become 35.</summary>
    public static bool IsNumeric(string? value) =>
        decimal.TryParse(value, NumericStyles, CultureInfo.InvariantCulture, out _);

    private const NumberStyles NumericStyles =
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint
        | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;
}

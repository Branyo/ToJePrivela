using System.ComponentModel.DataAnnotations;

namespace ToJePrivela.Application.Common;

/// <summary>
/// Checks a request against its data annotations, so a use case rejects bad input itself instead of
/// trusting a caller (such as ASP.NET model validation) to have done it.
/// </summary>
public static class RequestValidator
{
    /// <returns>A validation error naming every broken rule, or null when the request is valid.</returns>
    public static Error? Validate<TRequest>(TRequest? request) where TRequest : class
    {
        if (request is null)
        {
            return Error.Validation("Request.Missing", "The request is required.");
        }

        var results = new List<ValidationResult>();

        return Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true)
            ? null
            : Error.Validation("Request.Invalid", string.Join(" ", results.Select(result => result.ErrorMessage)));
    }
}

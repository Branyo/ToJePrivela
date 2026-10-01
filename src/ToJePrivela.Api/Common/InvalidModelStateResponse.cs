using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Api.Common;

/// <summary>
/// ASP.NET rejects a request whose body cannot be read or whose annotations fail before the use case runs.
/// That answer takes the same shape as the use case's own <see cref="RequestErrors.Invalid"/>, code included.
/// </summary>
public static class InvalidModelStateResponse
{
    public static IActionResult Create(ActionContext context)
    {
        var messages = context.ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                ? error.Exception?.Message ?? "The request is not valid."
                : error.ErrorMessage);

        return ResultExtensions.ToProblem(RequestErrors.Invalid(messages));
    }
}

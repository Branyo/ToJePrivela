using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Api.Common;

/// <summary>Turns a use-case result into an HTTP response, so controllers hold no branching logic.</summary>
public static class ResultExtensions
{
    public static ActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new NoContentResult() : ToProblem(result.Error);

    public static ActionResult<TValue> ToActionResult<TValue>(this Result<TValue> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : ToProblem(result.Error);

    public static ActionResult<TValue> ToCreatedResult<TValue>(
        this Result<TValue> result,
        string actionName,
        Func<TValue, object> routeValues) =>
        result.IsSuccess
            ? new CreatedAtActionResult(actionName, null, routeValues(result.Value), result.Value)
            : ToProblem(result.Error);

    public static ObjectResult ToProblem(Error error)
    {
        var problem = ToProblemDetails(error);
        return new ObjectResult(problem) { StatusCode = problem.Status };
    }

    /// <summary>
    /// The one shape every error answer has, whether a use case, ASP.NET model validation or an exception
    /// handler produced it: status and title from <see cref="Error.Type"/>, the message as detail and a
    /// <c>code</c> extension the frontend translates.
    /// </summary>
    public static ProblemDetails ToProblemDetails(Error error) => new()
    {
        Status = ToStatusCode(error.Type),
        Title = ToTitle(error.Type),
        Detail = error.Message,
        Extensions = { ["code"] = error.Code }
    };

    private static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string ToTitle(ErrorType type) => type switch
    {
        ErrorType.Validation => "Invalid request",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unavailable => "Service unavailable",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Forbidden",
        _ => "Unexpected error"
    };
}

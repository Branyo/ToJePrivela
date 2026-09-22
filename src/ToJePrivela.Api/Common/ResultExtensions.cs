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
        var statusCode = ToStatusCode(error.Type);

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = ToTitle(error.Type),
            Detail = error.Message,
            Extensions = { ["code"] = error.Code }
        };

        return new ObjectResult(problem) { StatusCode = statusCode };
    }

    private static int ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string ToTitle(ErrorType type) => type switch
    {
        ErrorType.Validation => "Invalid request",
        ErrorType.NotFound => "Resource not found",
        ErrorType.Conflict => "Conflict",
        ErrorType.Unavailable => "Service unavailable",
        _ => "Unexpected error"
    };
}

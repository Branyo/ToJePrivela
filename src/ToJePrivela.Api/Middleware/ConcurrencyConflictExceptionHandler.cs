using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Application.Abstractions.Persistence;

namespace ToJePrivela.Api.Middleware;

/// <summary>
/// A change that lost a race with another request (an edit or a delete of a question someone was
/// just shown) is a conflict the client can retry, not a server error.
/// </summary>
public sealed class ConcurrencyConflictExceptionHandler : IExceptionHandler
{
    public const string Code = "Concurrency.Conflict";

    private readonly ILogger<ConcurrencyConflictExceptionHandler> _logger;

    public ConcurrencyConflictExceptionHandler(ILogger<ConcurrencyConflictExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ConcurrencyConflictException conflict)
        {
            return false;
        }

        _logger.LogWarning(conflict, "Concurrent change detected.");

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Conflict",
            Detail = "The resource was changed by another request. Reload it and try again.",
            Extensions = { ["code"] = Code }
        };

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}

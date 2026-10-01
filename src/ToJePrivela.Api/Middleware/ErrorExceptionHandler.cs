using Microsoft.AspNetCore.Diagnostics;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Api.Middleware;

/// <summary>Turns one kind of exception into an <see cref="Error"/>, answered like any use-case failure.</summary>
public abstract class ErrorExceptionHandler<TException> : IExceptionHandler where TException : Exception
{
    private readonly ILogger _logger;

    protected ErrorExceptionHandler(ILogger logger)
    {
        _logger = logger;
    }

    protected abstract string LogMessage { get; }

    protected abstract Error ToError(TException exception);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not TException handled)
        {
            return false;
        }

        _logger.LogWarning(handled, "{Reason}", LogMessage);

        var problem = ResultExtensions.ToProblemDetails(ToError(handled));

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}

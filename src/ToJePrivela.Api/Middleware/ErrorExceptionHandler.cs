using Microsoft.AspNetCore.Diagnostics;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Accounts;
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

    /// <summary>The error to answer with; a handler that has to look something up first overrides this.</summary>
    protected virtual ValueTask<Error> ToErrorAsync(
        TException exception,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        ValueTask.FromResult(ToError(exception));

    /// <summary>
    /// <see cref="AccountErrors.UnknownAccount"/> when the signed-in login was deleted during the request (an admin
    /// deleted it), so the client signs out at once; otherwise <paramref name="error"/>. A lookup that fails (the
    /// deletion still holding SQLite's write lock) answers <paramref name="error"/> too: this handler is there to keep
    /// the request from ending in a 500.
    /// </summary>
    protected async ValueTask<Error> UnknownAccountOrAsync(
        Error error,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (HttpCurrentAccount.IdOf(httpContext.User) is not { } accountId)
        {
            return error;
        }

        try
        {
            return await SignedInLogin.GoneOrAsync(
                httpContext.RequestServices.GetRequiredService<IAccountRepository>(),
                accountId,
                error,
                cancellationToken);
        }
        catch (Exception lookupFailure) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(lookupFailure, "Could not check whether the signed-in login still exists.");
            return error;
        }
    }

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

        var problem = ResultExtensions.ToProblemDetails(await ToErrorAsync(handled, httpContext, cancellationToken));

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}

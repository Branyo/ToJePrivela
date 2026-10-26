using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Api.Middleware;

/// <summary>
/// A change that lost a race with another request (an edit or a delete of a question someone was
/// just shown) is a conflict the client can retry, not a server error. A change of a row an admin's deletion of the
/// signed-in login already removed ends here too; it gets the same 401 its next request would.
/// </summary>
public sealed class ConcurrencyConflictExceptionHandler : ErrorExceptionHandler<ConcurrencyConflictException>
{
    public const string Code = "Concurrency.Conflict";

    public ConcurrencyConflictExceptionHandler(ILogger<ConcurrencyConflictExceptionHandler> logger) : base(logger)
    {
    }

    protected override string LogMessage => "Concurrent change detected.";

    protected override Error ToError(ConcurrencyConflictException exception) =>
        Error.Conflict(Code, "The resource was changed by another request. Reload it and try again.");

    protected override ValueTask<Error> ToErrorAsync(
        ConcurrencyConflictException exception,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        UnknownAccountOrAsync(ToError(exception), httpContext, cancellationToken);
}

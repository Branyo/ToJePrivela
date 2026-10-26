using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Api.Middleware;

/// <summary>
/// A change pointing at a row deleted meanwhile lost a race, so it is a conflict, not a server error. When the deleted
/// row is the signed-in login itself (an admin deleted it during the request), the answer is the same 401 its next
/// request would get, so the client signs out at once.
/// </summary>
public sealed class MissingReferenceExceptionHandler : ErrorExceptionHandler<MissingReferenceException>
{
    public const string Code = "Concurrency.MissingReference";

    public MissingReferenceExceptionHandler(ILogger<MissingReferenceExceptionHandler> logger) : base(logger)
    {
    }

    protected override string LogMessage => "Change refers to a deleted row.";

    protected override Error ToError(MissingReferenceException exception) =>
        Error.Conflict(Code, "Something the change refers to was deleted meanwhile. Reload and try again.");

    protected override ValueTask<Error> ToErrorAsync(
        MissingReferenceException exception,
        HttpContext httpContext,
        CancellationToken cancellationToken) =>
        UnknownAccountOrAsync(ToError(exception), httpContext, cancellationToken);
}

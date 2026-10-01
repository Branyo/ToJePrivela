using ToJePrivela.Application.Common;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Api.Middleware;

/// <summary>Last line of defence: a broken invariant is a bad request, not a server error.</summary>
public sealed class DomainExceptionHandler : ErrorExceptionHandler<DomainException>
{
    public const string Code = "Domain.RuleViolated";

    public DomainExceptionHandler(ILogger<DomainExceptionHandler> logger) : base(logger)
    {
    }

    protected override string LogMessage => "Domain rule violated.";

    protected override Error ToError(DomainException exception) => Error.Validation(Code, exception.Message);
}

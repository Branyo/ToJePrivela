using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

namespace ToJePrivela.Api.Common;

/// <summary>
/// Answers a request a rate limit refused (429) like any other error, with a <c>code</c> the frontend translates and,
/// when the limiter knows it, how many seconds to wait (<c>retryAfterSeconds</c> and the <c>Retry-After</c> header).
/// </summary>
public static class RateLimitRejection
{
    public const string Code = "RateLimit.Exceeded";
    public const string RetryAfterSecondsExtension = "retryAfterSeconds";

    public static ProblemDetails ToProblemDetails(TimeSpan? retryAfter)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Too many requests",
            Detail = "Too many attempts in a short time; wait a moment and try again.",
            Extensions = { ["code"] = Code }
        };

        if (retryAfter is { } wait)
        {
            problem.Extensions[RetryAfterSecondsExtension] = RetryAfterSeconds(wait);
        }

        return problem;
    }

    public static async ValueTask WriteAsync(HttpContext httpContext, RateLimitLease lease, CancellationToken cancellationToken)
    {
        TimeSpan? retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var wait) ? wait : null;

        if (retryAfter is { } known)
        {
            httpContext.Response.Headers.RetryAfter = RetryAfterSeconds(known).ToString(CultureInfo.InvariantCulture);
        }

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await httpContext.Response.WriteAsJsonAsync(
            ToProblemDetails(retryAfter),
            options: null,
            contentType: "application/problem+json",
            cancellationToken);
    }

    /// <summary>Whole seconds, rounded up and at least 1, so waiting that long is always enough.</summary>
    private static int RetryAfterSeconds(TimeSpan wait) => Math.Max(1, (int)Math.Ceiling(wait.TotalSeconds));
}

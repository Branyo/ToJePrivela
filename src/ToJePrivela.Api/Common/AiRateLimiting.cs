using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Api.Common;

/// <summary>
/// The one fixed window (<see cref="AiRateLimitOptions"/>) shared by every request that pays for AI calls, however many
/// endpoints those come from.
/// </summary>
public sealed class AiRateLimiter : IDisposable
{
    private readonly FixedWindowRateLimiter _limiter;

    public AiRateLimiter(AiRateLimitOptions options)
    {
        _limiter = new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            Window = TimeSpan.FromSeconds(options.WindowSeconds),
            QueueLimit = 0
        });
    }

    public RateLimitLease Acquire() => _limiter.AttemptAcquire();

    public void Dispose() => _limiter.Dispose();
}

/// <summary>
/// Counts the action towards <see cref="AiRateLimiter"/> and answers 429 once the window is used up. It runs after
/// authorization and model validation, so refused or invalid requests are never counted, and it skips a request whose
/// body says it calls no AI (<see cref="IMayUseAi"/>), such as a category created with both names and imported questions.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LimitAiUseAttribute : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionArguments.Values.OfType<IMayUseAi>().Any(request => !request.UsesAi()))
        {
            await next();
            return;
        }

        using var lease = context.HttpContext.RequestServices.GetRequiredService<AiRateLimiter>().Acquire();

        if (!lease.IsAcquired)
        {
            context.Result = Rejection(context.HttpContext, lease);
            return;
        }

        await next();
    }

    private static ObjectResult Rejection(HttpContext httpContext, RateLimitLease lease)
    {
        TimeSpan? retryAfter = lease.TryGetMetadata(MetadataName.RetryAfter, out var wait) ? wait : null;
        var problem = RateLimitRejection.ToProblemDetails(retryAfter);

        if (problem.Extensions.TryGetValue(RateLimitRejection.RetryAfterSecondsExtension, out var seconds))
        {
            httpContext.Response.Headers.RetryAfter = Convert.ToString(seconds, CultureInfo.InvariantCulture);
        }

        return new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status429TooManyRequests,
            ContentTypes = { "application/problem+json" }
        };
    }
}

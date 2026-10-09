using Serilog.Events;

namespace ToJePrivela.Api.Common;

/// <summary>The level Serilog's request logging writes each request at.</summary>
public static class RequestLogLevel
{
    /// <summary>
    /// Server failures (an exception or any 5xx, a failing health check's 503 included) are errors. Probes poll every
    /// few seconds, so their successful answers stay out of the Information log. Everything else is Information.
    /// </summary>
    public static LogEventLevel For(HttpContext context, Exception? exception)
    {
        var status = context.Response.StatusCode;

        if (exception is not null || status >= StatusCodes.Status500InternalServerError)
        {
            return LogEventLevel.Error;
        }

        return status < StatusCodes.Status400BadRequest && context.IsProbe()
            ? LogEventLevel.Verbose
            : LogEventLevel.Information;
    }
}

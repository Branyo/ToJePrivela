using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Api.Common;

/// <summary>
/// The sign-in limit (<see cref="SignInRateLimitOptions"/>) counts per client address <em>and</em> login name: the
/// people of one party behind the same address each get their own attempts, while guessing one login's password stays
/// slow. The rate limiter picks a partition before the body is bound, so <see cref="UseSignInLoginNames"/> reads the
/// name out of the JSON body first.
/// </summary>
public static class SignInRateLimiting
{
    /// <summary>Larger bodies are not read; they count towards the address's nameless partition.</summary>
    public const int MaxReadBodyBytes = 4096;

    private const string LoginNameItem = "ToJePrivela.SignInLoginName";

    /// <summary>Goes after routing (the endpoint is known) and before <c>UseRateLimiter</c>.</summary>
    public static IApplicationBuilder UseSignInLoginNames(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (IsSignInEndpoint(context))
            {
                context.Items[LoginNameItem] = await ReadLoginNameAsync(context.Request, context.RequestAborted);
            }

            await next(context);
        });

    public static RateLimitPartition<string> Partition(HttpContext context, SignInRateLimitOptions options) =>
        RateLimitPartition.GetFixedWindowLimiter(
            PartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.PermitLimit,
                Window = TimeSpan.FromSeconds(options.WindowSeconds),
                QueueLimit = 0
            });

    /// <summary>The address and the login name as <see cref="NameKeys.Of"/> compares it ("Brano " counts as "brano").</summary>
    public static string PartitionKey(HttpContext context)
    {
        var address = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var name = context.Items[LoginNameItem] as string ?? string.Empty;
        return $"{address}|{name}";
    }

    /// <summary>
    /// The body's <c>name</c> as a name key, or empty when there is none to read: the request then shares the
    /// address's nameless partition, so leaving the name out never escapes the limit. The body is left readable for
    /// model binding.
    /// </summary>
    public static async Task<string> ReadLoginNameAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        if (request.ContentLength is not (> 0 and <= MaxReadBodyBytes))
        {
            return string.Empty;
        }

        request.EnableBuffering();

        try
        {
            using var body = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken);

            if (body.RootElement.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            // Model binding matches property names case-insensitively, so this does too.
            foreach (var property in body.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, "name", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String)
                {
                    return NameKeys.Of(property.Value.GetString()!);
                }
            }

            return string.Empty;
        }
        catch (JsonException)
        {
            // Model binding answers the malformed body with a 400.
            return string.Empty;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    private static bool IsSignInEndpoint(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
            == SignInRateLimitOptions.PolicyName;
}

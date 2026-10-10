using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using ToJePrivela.Domain.Common;
using ToJePrivela.Identity.Tokens;
using MvcJsonOptions = Microsoft.AspNetCore.Mvc.JsonOptions;

namespace ToJePrivela.Api.Common;

/// <summary>
/// The sign-in limits (<see cref="SignInRateLimitOptions"/>). The endpoint policy counts per client address <em>and</em>
/// login name: the people of one party behind the same address each get their own attempts, while guessing one login's
/// password stays slow. <see cref="SignInAddressLimiter"/> also caps the attempts one address makes on those endpoints,
/// so it cannot try many names, or create many logins, at speed; a successful sign-in is not counted there, so people
/// who know their passwords never use up their address's cap. The rate limiter picks a partition before the body is
/// bound, so <see cref="UseSignInLimits"/> reads the name out of the JSON body first. An endpoint that needs a sign-in
/// (changing the password) counts per signed-in login instead, whatever the address, and never towards the address's
/// cap: its body carries no name, guessing the current password from many addresses must stay as slow as from one, and
/// people behind one address must not use up each other's sign-ins.
/// </summary>
public static class SignInRateLimiting
{
    /// <summary>Larger bodies are not read; they count towards the address's nameless partition.</summary>
    public const int MaxReadBodyBytes = 4096;

    private const string LoginNameItem = "ToJePrivela.SignInLoginName";
    private const string AccountItem = "ToJePrivela.SignInAccount";

    /// <summary>
    /// Goes after authentication and routing (the signed-in login and the endpoint are known) and before
    /// <c>UseRateLimiter</c>. A signed-in request skips the address's cap. Otherwise an address over its cap is refused
    /// before its body is read, and the request is counted towards the cap once it is answered, unless the endpoint is
    /// marked <see cref="SuccessIsFreeAttribute"/> and the answer succeeded.
    /// </summary>
    public static IApplicationBuilder UseSignInLimits(this IApplicationBuilder app)
    {
        var addressLimiter = app.ApplicationServices.GetRequiredService<SignInAddressLimiter>();
        var jsonOptions = app.ApplicationServices.GetRequiredService<IOptions<MvcJsonOptions>>().Value.JsonSerializerOptions;

        return app.Use(async (context, next) =>
        {
            if (!IsSignInEndpoint(context))
            {
                await next(context);
                return;
            }

            // Counted per login only (the endpoint policy). The address's cap is for trying many names, and people
            // behind one address must not lose their sign-ins to someone mistyping a current password, or the other way.
            if (SignedInAccount(context) is { } account)
            {
                context.Items[AccountItem] = account;
                await next(context);
                return;
            }

            var address = Address(context);

            using (var check = addressLimiter.Check(address))
            {
                if (!check.IsAcquired)
                {
                    await RateLimitRejection.WriteAsync(context, check, context.RequestAborted);
                    return;
                }
            }

            var succeeded = false;

            try
            {
                context.Items[LoginNameItem] = await ReadLoginNameAsync(context.Request, jsonOptions, context.RequestAborted);
                await next(context);
                succeeded = context.Response.StatusCode is >= 200 and < 300;
            }
            finally
            {
                if (!(succeeded && context.GetEndpoint()?.Metadata.GetMetadata<SuccessIsFreeAttribute>() is not null))
                {
                    addressLimiter.Count(address);
                }
            }
        });
    }

    public static RateLimitPartition<string> Partition(HttpContext context, SignInRateLimitOptions options) =>
        RateLimitPartition.GetFixedWindowLimiter(
            PartitionKey(context),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.PermitLimit,
                Window = TimeSpan.FromSeconds(options.WindowSeconds),
                QueueLimit = 0
            });

    /// <summary>
    /// The signed-in login on an endpoint that needs one; otherwise the address and the login name as
    /// <see cref="NameKeys.Of"/> compares it ("Brano " counts as "brano").
    /// </summary>
    public static string PartitionKey(HttpContext context)
    {
        if (context.Items[AccountItem] is string account)
        {
            return $"account:{account}";
        }

        var name = context.Items[LoginNameItem] as string ?? string.Empty;
        return $"{Address(context)}|{name}";
    }

    /// <summary>
    /// The body's <c>name</c> as a name key, or empty when there is none to read: the request then shares the
    /// address's nameless partition, so leaving the name out never escapes the limit. The body is read with the
    /// options model binding uses, so both see the same name (property casing, a repeated <c>name</c>); it is left
    /// readable for model binding.
    /// </summary>
    public static async Task<string> ReadLoginNameAsync(
        HttpRequest request,
        JsonSerializerOptions jsonOptions,
        CancellationToken cancellationToken)
    {
        if (request.ContentLength is not (> 0 and <= MaxReadBodyBytes))
        {
            return string.Empty;
        }

        request.EnableBuffering();

        try
        {
            var body = await JsonSerializer.DeserializeAsync<NameOnly>(request.Body, jsonOptions, cancellationToken);

            return body?.Name is { } name ? NameKeys.Of(name) : string.Empty;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or ArgumentException)
        {
            // Model binding answers a malformed body with a 400. ArgumentException: a name that is not valid Unicode
            // (a lone surrogate) cannot be normalized into a key.
            return string.Empty;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }

    private static string Address(HttpContext context) => context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    /// <summary>
    /// The signed-in login's id, on an endpoint that needs a sign-in. Authentication has run, so the user is set only
    /// for a token that is still valid; anything else counts towards the address's nameless partition and is then
    /// answered 401.
    /// </summary>
    private static string? SignedInAccount(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is null
        && context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(AccessTokenClaims.Subject)?.Value
            : null;

    private static bool IsSignInEndpoint(HttpContext context) =>
        context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
            == SignInRateLimitOptions.PolicyName;

    private sealed class NameOnly
    {
        public string? Name { get; init; }
    }
}

/// <summary>On a sign-in endpoint: an answer that succeeds does not count towards the address's cap.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SuccessIsFreeAttribute : Attribute;

/// <summary>
/// Counts, per client address, the sign-in attempts <see cref="SignInRateLimiting.UseSignInLimits"/> reports, in fixed
/// windows of <see cref="SignInRateLimitOptions.WindowSeconds"/>, up to <see cref="SignInRateLimitOptions.AddressPermitLimit"/>.
/// </summary>
public sealed class SignInAddressLimiter : IDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter;

    public SignInAddressLimiter(SignInRateLimitOptions options)
    {
        _limiter = PartitionedRateLimiter.Create<string, string>(address => RateLimitPartition.GetFixedWindowLimiter(
            address,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = options.AddressPermitLimit,
                Window = TimeSpan.FromSeconds(options.WindowSeconds),
                QueueLimit = 0
            }));
    }

    /// <summary>Acquired while the address has attempts left in this window; consumes none of them.</summary>
    public RateLimitLease Check(string address) => _limiter.AttemptAcquire(address, permitCount: 0);

    public void Count(string address) => _limiter.AttemptAcquire(address).Dispose();

    public void Dispose() => _limiter.Dispose();
}

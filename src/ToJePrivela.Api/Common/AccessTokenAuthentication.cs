using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Accounts;
using ToJePrivela.Application.Common;
using ToJePrivela.Identity.Tokens;

namespace ToJePrivela.Api.Common;

public static class AccessTokenAuthentication
{
    /// <summary>Set when a valid token names a login that is gone, so the challenge can say so.</summary>
    private const string FailureItem = "ToJePrivela.AuthFailure";

    /// <summary>
    /// Accepts the access tokens issued at sign-in and requires one on every endpoint not marked
    /// <c>[AllowAnonymous]</c>. The login a token names is read on every request: a login that is gone is refused, and
    /// only a login that is admin <em>now</em> gets the admin role. A missing token (401) or a missing admin role (403)
    /// is answered like any other error.
    /// </summary>
    public static IServiceCollection AddAccessTokenAuthentication(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentAccount, HttpCurrentAccount>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Read when the first request comes in, so configuration (and the tests' settings) is complete by then.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = jwt.Value.CreateValidationParameters();
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = AttachStoredAccountAsync,
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        var error = context.HttpContext.Items[FailureItem] as Error ?? AccountErrors.Unauthenticated;
                        return WriteProblemAsync(context.HttpContext, error);
                    },
                    OnForbidden = context => WriteProblemAsync(context.HttpContext, AccountErrors.Forbidden)
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(AuthorizationPolicies.Admin, policy => policy
                .RequireAuthenticatedUser()
                .RequireRole(AccessTokenClaims.AdminRole));

        return services;
    }

    private static async Task AttachStoredAccountAsync(TokenValidatedContext context)
    {
        var subject = context.Principal?.FindFirst(AccessTokenClaims.Subject)?.Value;

        if (!int.TryParse(subject, NumberStyles.None, CultureInfo.InvariantCulture, out var id))
        {
            context.Fail("The access token names no login.");
            return;
        }

        var accounts = context.HttpContext.RequestServices.GetRequiredService<IAccountService>();
        var account = await accounts.GetByIdAsync(id, context.HttpContext.RequestAborted);

        if (account.IsFailure)
        {
            context.HttpContext.Items[FailureItem] = account.Error;
            context.Fail(account.Error.Message);
            return;
        }

        if (account.Value.IsAdmin)
        {
            context.Principal!.AddIdentity(new ClaimsIdentity(
                [new Claim(AccessTokenClaims.Role, AccessTokenClaims.AdminRole)],
                authenticationType: null,
                AccessTokenClaims.Name,
                AccessTokenClaims.Role));
        }
    }

    private static Task WriteProblemAsync(HttpContext httpContext, Error error)
    {
        var problem = ResultExtensions.ToProblemDetails(error);
        httpContext.Response.StatusCode = problem.Status!.Value;
        return httpContext.Response.WriteAsJsonAsync(problem);
    }
}

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
    /// <summary>
    /// Accepts the access tokens issued at sign-in and requires one on every endpoint not marked
    /// <c>[AllowAnonymous]</c>. A missing token (401) or a missing admin role (403) is answered like any other error.
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
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return WriteProblemAsync(context.HttpContext, AccountErrors.Unauthenticated);
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

    private static Task WriteProblemAsync(HttpContext httpContext, Error error)
    {
        var problem = ResultExtensions.ToProblemDetails(error);
        httpContext.Response.StatusCode = problem.Status!.Value;
        return httpContext.Response.WriteAsJsonAsync(problem);
    }
}

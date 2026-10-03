using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Identity.Facebook;
using ToJePrivela.Identity.Google;
using ToJePrivela.Identity.Tokens;

namespace ToJePrivela.Identity;

public static class DependencyInjection
{
    /// <summary>Google and Facebook sign-in (each offered only when configured) and the access tokens issued after it.</summary>
    public static IServiceCollection AddIdentityProviders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<GoogleOptions>()
            .Bind(configuration.GetSection(GoogleOptions.SectionName));

        services.AddOptions<FacebookOptions>()
            .Bind(configuration.GetSection(FacebookOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        services.AddSingleton<IGoogleIdTokenValidator, GoogleJsonWebSignatureValidator>();
        services.AddScoped<IExternalIdentityVerifier, GoogleIdentityVerifier>();

        services.AddHttpClient<FacebookIdentityVerifier>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<FacebookOptions>>().Value;
            client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
        });
        services.AddScoped<IExternalIdentityVerifier>(provider => provider.GetRequiredService<FacebookIdentityVerifier>());

        return services;
    }
}

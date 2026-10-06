using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Identity.Passwords;
using ToJePrivela.Identity.Tokens;

namespace ToJePrivela.Identity;

public static class DependencyInjection
{
    /// <summary>Password hashing for the logins and the access tokens issued once one signs in.</summary>
    public static IServiceCollection AddPasswordLogins(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        return services;
    }
}

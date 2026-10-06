using System.Net;
using ToJePrivela.Identity.Tokens;

namespace ToJePrivela.Api.Common;

/// <summary>
/// Refuses settings that are only safe on a developer's machine. <c>appsettings.Development.json</c> carries a signing
/// key that is committed to the repository: wherever it signs tokens, anyone can forge one for any login, admin
/// included. Development also turns on Swagger UI.
/// </summary>
public static class DeploymentSafety
{
    /// <summary>The key in <c>appsettings.Development.json</c>; a test keeps the two the same.</summary>
    public const string DevelopmentSigningKey = "development-only-signing-key-never-use-in-production";

    /// <summary>Outside Development the committed key must not be configured, wherever it was copied to.</summary>
    public static void EnsureNoDevelopmentSigningKey(IHostEnvironment environment, IConfiguration configuration)
    {
        if (!environment.IsDevelopment()
            && configuration[$"{JwtOptions.SectionName}:{nameof(JwtOptions.SigningKey)}"] == DevelopmentSigningKey)
        {
            throw new InvalidOperationException(
                $"Authentication:Jwt:SigningKey is the Development key, which is public, in the '{environment.EnvironmentName}' "
                + "environment. Set a random key of your own (user-secrets or Authentication__Jwt__SigningKey).");
        }
    }

    /// <summary>
    /// Development is for one machine: once the server has started, it stops at once when it listens on an address other
    /// machines can reach (e.g. <c>ASPNETCORE_ENVIRONMENT=Development</c> left on a deployed container).
    /// </summary>
    public static void StopDevelopmentReachableFromNetwork(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var reachable = ReachableFromNetwork(app.Urls);

            if (reachable.Count == 0)
            {
                return;
            }

            app.Logger.LogCritical(
                "Stopping: the Development environment listens on {Addresses}, which other machines can reach, and its "
                + "signing key is public. Run Development on localhost only, or set ASPNETCORE_ENVIRONMENT to Production.",
                string.Join(", ", reachable));
            Environment.ExitCode = 1;
            app.Lifetime.StopApplication();
        });
    }

    /// <summary>
    /// The addresses not bound to loopback (<c>localhost</c>, <c>127.0.0.1</c>, <c>[::1]</c>), so wildcards
    /// (<c>*</c>, <c>+</c>, <c>0.0.0.0</c>, <c>[::]</c>) and real host names count. Anything else, such as one that
    /// cannot be parsed, counts too: when in doubt the server stops.
    /// </summary>
    public static IReadOnlyList<string> ReachableFromNetwork(IEnumerable<string> addresses) =>
        addresses.Where(address => !IsLoopback(address)).ToList();

    private static bool IsLoopback(string address)
    {
        BindingAddress binding;

        try
        {
            binding = BindingAddress.Parse(address);
        }
        catch (Exception exception) when (exception is FormatException or InvalidOperationException)
        {
            return false;
        }

        var host = binding.Host.Trim('[', ']');

        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || (IPAddress.TryParse(host, out var ip) && IPAddress.IsLoopback(ip));
    }
}

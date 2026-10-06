using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using ToJePrivela.Api.Common;

namespace ToJePrivela.Api.Tests.Common;

public class DeploymentSafetyTests
{
    [Theory]
    [InlineData("http://localhost:5178")]
    [InlineData("https://LOCALHOST:7030")]
    [InlineData("http://127.0.0.1:5000")]
    [InlineData("http://[::1]:5000")]
    public void LoopbackAddresses_AreNotReachableFromNetwork(string address)
    {
        Assert.Empty(DeploymentSafety.ReachableFromNetwork([address]));
    }

    [Theory]
    [InlineData("http://*:8080")]
    [InlineData("http://+:80")]
    [InlineData("http://0.0.0.0:5000")]
    [InlineData("http://[::]:8080")]
    [InlineData("http://192.168.1.20:5000")]
    [InlineData("https://tojeprivela.example.com")]
    [InlineData("not an address")]
    public void OtherAddresses_AreReachableFromNetwork(string address)
    {
        Assert.Equal([address], DeploymentSafety.ReachableFromNetwork(["http://localhost:5178", address]));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void DevelopmentSigningKey_IsRefusedOutsideDevelopment(string environment)
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            DeploymentSafety.EnsureNoDevelopmentSigningKey(Environment(environment), KeyedWith(DeploymentSafety.DevelopmentSigningKey)));

        Assert.Contains("Development key", exception.Message);
    }

    [Fact]
    public void DevelopmentSigningKey_IsAllowedInDevelopment()
    {
        DeploymentSafety.EnsureNoDevelopmentSigningKey(
            Environment(Environments.Development),
            KeyedWith(DeploymentSafety.DevelopmentSigningKey));
    }

    [Fact]
    public void AnOwnSigningKey_IsAllowedEverywhere()
    {
        DeploymentSafety.EnsureNoDevelopmentSigningKey(
            Environment(Environments.Production),
            KeyedWith("a-production-key-nobody-else-knows-0123456789"));
    }

    [Fact]
    public void DevelopmentSigningKey_IsTheOneInTheDevelopmentSettings()
    {
        using var settings = JsonDocument.Parse(File.ReadAllText(DevelopmentSettingsPath()));

        var key = settings.RootElement.GetProperty("Authentication").GetProperty("Jwt").GetProperty("SigningKey").GetString();

        Assert.Equal(DeploymentSafety.DevelopmentSigningKey, key);
    }

    private static IHostEnvironment Environment(string name)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(name);
        return environment;
    }

    private static IConfiguration KeyedWith(string signingKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:Jwt:SigningKey"] = signingKey })
            .Build();

    private static string DevelopmentSettingsPath()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var path = Path.Combine(directory.FullName, "src", "ToJePrivela.Api", "appsettings.Development.json");

            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("src/ToJePrivela.Api/appsettings.Development.json was not found above the test output.");
    }
}

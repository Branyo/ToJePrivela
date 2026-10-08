using System.Net;
using System.Net.Http.Json;
using ToJePrivela.Api.Controllers;

namespace ToJePrivela.Api.Tests.Integration;

public class InfoEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _anonymous;

    public InfoEndpointsTests(ApiFactory factory)
    {
        _anonymous = factory.CreateAnonymousClient();
    }

    [Fact]
    public async Task Health_ReportsHealthyWithoutSigningIn()
    {
        var response = await _anonymous.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", (await response.Content.ReadFromJsonAsync<HealthResponse>())!.Status);
    }

    [Fact]
    public async Task Version_ServesTheAssemblyVersionWithoutSigningIn()
    {
        var response = await _anonymous.GetAsync("/api/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var version = (await response.Content.ReadFromJsonAsync<VersionResponse>())!.Version;
        Assert.False(string.IsNullOrWhiteSpace(version));
        Assert.DoesNotContain('+', version);
    }

    [Fact]
    public async Task Title_ServesTheApiNameWithoutSigningIn()
    {
        var response = await _anonymous.GetAsync("/api/title");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ToJePrivela API", (await response.Content.ReadFromJsonAsync<TitleResponse>())!.Title);
    }
}

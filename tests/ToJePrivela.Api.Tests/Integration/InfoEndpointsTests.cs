using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ToJePrivela.Application.Info;
using ToJePrivela.Application.Info.Dtos;

namespace ToJePrivela.Api.Tests.Integration;

public class InfoEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _anonymous;

    public InfoEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _anonymous = factory.CreateAnonymousClient();
    }

    [Fact]
    public async Task Health_ReportsHealthyWithoutSigningIn_WhenTheDatabaseAnswers()
    {
        var response = await _anonymous.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Info_ServesTitleAndVersionWithoutSigningIn()
    {
        var response = await _anonymous.GetAsync("/api/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var info = (await response.Content.ReadFromJsonAsync<ApiInfoDto>())!;
        Assert.Equal(ApiInfoService.ApiTitle, info.Title);
        Assert.NotEqual("unknown", info.Version);
        Assert.Matches(@"^\d+\.\d+\.\d+", info.Version);
        Assert.DoesNotContain('+', info.Version);
    }

    [Fact]
    public async Task Info_IsAlsoServedToSignedInClients()
    {
        var response = await _factory.CreateClient().GetAsync("/api/info");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OtherEndpoints_StillRequireSigningIn()
    {
        var response = await _anonymous.GetAsync("/api/players");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_AnswersServiceUnavailable_WhenACheckFails()
    {
        using var failing = new FailingHealthFactory();

        var response = await failing.CreateAnonymousClient().GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    /// <summary>A host with its own database whose health checks include one that fails, like an unreachable database.</summary>
    private sealed class FailingHealthFactory : ApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services => services.Configure<HealthCheckServiceOptions>(options =>
                options.Registrations.Add(new HealthCheckRegistration(
                    "failing", _ => new FailingCheck(), failureStatus: null, tags: null))));
        }
    }

    private sealed class FailingCheck : IHealthCheck
    {
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(HealthCheckResult.Unhealthy("database unreachable"));
    }
}

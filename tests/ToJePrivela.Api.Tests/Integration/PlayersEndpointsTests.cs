using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Application.Players.Dtos;

namespace ToJePrivela.Api.Tests.Integration;

public class PlayersEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public PlayersEndpointsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetPlayers_ReturnsTheSeededPlayers()
    {
        var players = await _client.GetFromJsonAsync<List<PlayerDto>>("/api/players");

        Assert.NotNull(players);
        Assert.Contains(players!, player => player.Name == "Admin");
    }

    [Fact]
    public async Task GetPlayer_ReturnsProblemDetailsForUnknownId()
    {
        var response = await _client.GetAsync("/api/players/9999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Resource not found", problem!.Title);
    }

    [Fact]
    public async Task PostPlayer_CreatesAndExposesTheNewPlayer()
    {
        var name = $"Player-{Guid.NewGuid():N}"[..20];

        var response = await _client.PostAsJsonAsync("/api/players", new { name });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var created = await response.Content.ReadFromJsonAsync<PlayerDto>();
        Assert.Equal(name, created!.Name);

        var fetched = await _client.GetFromJsonAsync<PlayerDto>($"/api/players/{created.Id}");
        Assert.Equal(name, fetched!.Name);
    }

    [Fact]
    public async Task PostPlayer_RejectsDuplicateName()
    {
        var response = await _client.PostAsJsonAsync("/api/players", new { name = "Admin" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostPlayer_RejectsTooShortName()
    {
        var response = await _client.PostAsJsonAsync("/api/players", new { name = "A" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeletePlayer_RemovesThePlayer()
    {
        var name = $"Temp-{Guid.NewGuid():N}"[..18];
        var created = await (await _client.PostAsJsonAsync("/api/players", new { name }))
            .Content.ReadFromJsonAsync<PlayerDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/players/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/players/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}

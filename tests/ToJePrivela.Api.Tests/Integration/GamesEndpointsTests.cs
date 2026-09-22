using System.Net;
using System.Net.Http.Json;
using ToJePrivela.Application.Games.Dtos;

namespace ToJePrivela.Api.Tests.Integration;

public class GamesEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public GamesEndpointsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostGame_StartsAGameForTheGivenPlayers()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { playerIds = new[] { 1, 2 } });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var game = await response.Content.ReadFromJsonAsync<GameDto>();
        Assert.Equal([1, 2], game!.PlayerIds);
        Assert.NotNull(game.Started);
        Assert.Null(game.Finished);
    }

    [Fact]
    public async Task PostGame_RejectsUnknownPlayers()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { playerIds = new[] { 1, 9999 } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostGame_RejectsASinglePlayer()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { playerIds = new[] { 1 } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetGameDetails_ListsThePlayerNames()
    {
        var game = await CreateGameAsync();

        var details = await _client.GetFromJsonAsync<GameDetailsDto>($"/api/games/{game.Id}/details");

        Assert.Equal(2, details!.Players.Count);
        Assert.All(details.Players, player => Assert.False(string.IsNullOrWhiteSpace(player.Name)));
        Assert.All(details.Players, player => Assert.Equal(0, player.BadPoints));
    }

    [Fact]
    public async Task PutGame_FinishesTheGame()
    {
        var game = await CreateGameAsync();
        var finished = DateTime.UtcNow.AddHours(1);

        var response = await _client.PutAsJsonAsync(
            $"/api/games/{game.Id}",
            new { started = game.Started, finished });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var updated = await _client.GetFromJsonAsync<GameDto>($"/api/games/{game.Id}");
        Assert.NotNull(updated!.Finished);
    }

    [Fact]
    public async Task PutGame_RejectsAnEndBeforeTheStart()
    {
        var game = await CreateGameAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/games/{game.Id}",
            new { started = game.Started, finished = game.Started!.Value.AddHours(-2) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetGames_ListsCreatedGames()
    {
        await CreateGameAsync();

        var games = await _client.GetFromJsonAsync<List<GameDto>>("/api/games");

        Assert.NotEmpty(games!);
    }

    [Fact]
    public async Task DeleteGame_RemovesTheGame()
    {
        var game = await CreateGameAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/games/{game.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/games/{game.Id}")).StatusCode);
    }

    private async Task<GameDto> CreateGameAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { playerIds = new[] { 1, 2 } });
        return (await response.Content.ReadFromJsonAsync<GameDto>())!;
    }
}

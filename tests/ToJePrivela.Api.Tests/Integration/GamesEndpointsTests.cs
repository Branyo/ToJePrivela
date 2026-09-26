using System.Net;
using System.Net.Http.Json;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Questions.Dtos;

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

    [Fact]
    public async Task PostGame_AcceptsTwelvePlayersAndABadCardLimit()
    {
        var playerIds = new List<int>();
        for (var i = 0; i < 12; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/players", new { name = $"Hrac {Guid.NewGuid():N}"[..20] });
            playerIds.Add((await response.Content.ReadFromJsonAsync<PlayerDto>())!.Id);
        }

        var created = await _client.PostAsJsonAsync("/api/games", new { playerIds, badCardLimit = 5 });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(5, (await created.Content.ReadFromJsonAsync<GameDto>())!.BadCardLimit);
    }

    [Fact]
    public async Task PostBadCard_AddsTheQuestionsBadPointsAndFinishesAtTheLimit()
    {
        var game = await CreateGameAsync(badCardLimit: 2);
        var question = await CreateQuestionAsync(badPoints: 4);

        var first = await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = 2, questionId = question.Id });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var details = (await first.Content.ReadFromJsonAsync<GameDetailsDto>())!;
        var player = details.Players.Single(p => p.PlayerId == 2);
        Assert.Equal((1, 4), (player.BadCards, player.BadPoints));
        Assert.Null(details.Finished);

        var second = await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = 2, questionId = question.Id });
        Assert.NotNull((await second.Content.ReadFromJsonAsync<GameDetailsDto>())!.Finished);

        var third = await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = 1, questionId = question.Id });
        Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
    }

    [Fact]
    public async Task PostBadCard_RejectsAPlayerOutsideTheGame()
    {
        var game = await CreateGameAsync();
        var question = await CreateQuestionAsync(badPoints: 2);

        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = 3, questionId = question.Id });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostFinish_FinishesTheGameOnce()
    {
        var game = await CreateGameAsync();

        var response = await _client.PostAsync($"/api/games/{game.Id}/finish", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull((await response.Content.ReadFromJsonAsync<GameDetailsDto>())!.Finished);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsync($"/api/games/{game.Id}/finish", null)).StatusCode);
    }

    private async Task<GameDto> CreateGameAsync(int badCardLimit = 3)
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { playerIds = new[] { 1, 2 }, badCardLimit });
        return (await response.Content.ReadFromJsonAsync<GameDto>())!;
    }

    private async Task<QuestionDto> CreateQuestionAsync(int badPoints)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/questions",
            new { text = $"How many cars in {Guid.NewGuid():N}?", answer = "42", categoryId = 1, badPoints });
        return (await response.Content.ReadFromJsonAsync<QuestionDto>())!;
    }
}

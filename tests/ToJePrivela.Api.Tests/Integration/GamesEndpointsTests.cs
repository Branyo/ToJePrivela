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
    public async Task GetGame_ReturnsTheStartInUtc()
    {
        var game = await CreateGameAsync();

        var json = await _client.GetStringAsync($"/api/games/{game.Id}");

        Assert.Matches("\"started\":\"[^\"]+Z\"", json);
    }

    [Fact]
    public async Task PutGame_StoresATimeSentWithAnOffsetInUtc()
    {
        var game = await CreateGameAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/games/{game.Id}",
            new { started = "2026-09-30T10:00:00+02:00" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var json = await _client.GetStringAsync($"/api/games/{game.Id}");
        Assert.Contains("\"started\":\"2026-09-30T08:00:00Z\"", json);
    }

    [Fact]
    public async Task PutGame_RejectsAStartInTheFuture()
    {
        var game = await CreateGameAsync();

        var response = await _client.PutAsJsonAsync(
            $"/api/games/{game.Id}",
            new { started = DateTime.UtcNow.AddMinutes(5) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Game.StartsInFuture", await ProblemResponse.CodeOf(response));
        var stored = await _client.GetFromJsonAsync<GameDto>($"/api/games/{game.Id}");
        Assert.Equal(game.Started, stored!.Started);
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
        Assert.Equal((1, true), (player.Rank, player.IsLoser));
        Assert.Equal((2, false), details.Players.Where(p => p.PlayerId != 2).Select(p => (p.Rank, p.IsLoser)).Single());

        var second = await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = 2, questionId = question.Id });
        Assert.NotNull((await second.Content.ReadFromJsonAsync<GameDetailsDto>())!.Finished);

        var third = await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = 1, questionId = question.Id });
        Assert.Equal(HttpStatusCode.Conflict, third.StatusCode);
    }

    [Fact]
    public async Task PostBadCard_InAChooserGameUsesTheChosenBadPoints()
    {
        var created = await _client.PostAsJsonAsync("/api/games", new { playerIds = new[] { 1, 2 }, badPointsMode = "Chooser" });
        var game = (await created.Content.ReadFromJsonAsync<GameDto>())!;
        var question = await CreateQuestionAsync(badPoints: 4);

        Assert.Equal("Chooser", game.BadPointsMode);

        var withoutPoints = await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = 2, questionId = question.Id });
        Assert.Equal(HttpStatusCode.BadRequest, withoutPoints.StatusCode);

        var tooMany = await _client.PostAsJsonAsync(
            $"/api/games/{game.Id}/bad-cards",
            new { playerId = 2, questionId = question.Id, badPoints = 6 });
        Assert.Equal(HttpStatusCode.BadRequest, tooMany.StatusCode);

        var card = await _client.PostAsJsonAsync(
            $"/api/games/{game.Id}/bad-cards",
            new { playerId = 2, questionId = question.Id, badPoints = 2 });
        Assert.Equal(HttpStatusCode.OK, card.StatusCode);
        var player = (await card.Content.ReadFromJsonAsync<GameDetailsDto>())!.Players.Single(p => p.PlayerId == 2);
        Assert.Equal(2, player.BadPoints);
    }

    [Fact]
    public async Task PostBadCard_InAQuestionGameRejectsClientBadPoints()
    {
        var game = await CreateGameAsync();
        var question = await CreateQuestionAsync(badPoints: 4);

        Assert.Equal("Question", game.BadPointsMode);

        var response = await _client.PostAsJsonAsync(
            $"/api/games/{game.Id}/bad-cards",
            new { playerId = 2, questionId = question.Id, badPoints = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("Dice")]
    [InlineData("7")]
    public async Task PostGame_RejectsAnUnknownBadPointsMode(string mode)
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { playerIds = new[] { 1, 2 }, badPointsMode = mode });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostGame_RejectsABadCardLimitOfOne()
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { playerIds = new[] { 1, 2 }, badCardLimit = 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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
    public async Task PostDouble_IsCountedBeforeTheCardThatFinishesTheGame()
    {
        var game = await CreateGameAsync(badCardLimit: 2);
        var question = await CreateQuestionAsync(badPoints: 3);
        await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = 2, questionId = question.Id });

        var doubled = await _client.PostAsJsonAsync($"/api/games/{game.Id}/doubles", new { playerId = 1 });
        Assert.Equal(HttpStatusCode.OK, doubled.StatusCode);
        Assert.Equal(1, (await doubled.Content.ReadFromJsonAsync<GameDetailsDto>())!.Players.Single(p => p.PlayerId == 1).Doubles);

        var card = await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = 2, questionId = question.Id });
        var details = (await card.Content.ReadFromJsonAsync<GameDetailsDto>())!;
        Assert.NotNull(details.Finished);
        Assert.Equal(-1, details.Players.Single(p => p.PlayerId == 1).FinalBadPoints);
        Assert.Equal(6, details.Players.Single(p => p.PlayerId == 2).FinalBadPoints);

        var late = await _client.PostAsJsonAsync($"/api/games/{game.Id}/doubles", new { playerId = 1 });
        Assert.Equal(HttpStatusCode.Conflict, late.StatusCode);
    }

    [Fact]
    public async Task DeleteDouble_TakesBackOneDoubleUntilNoneAreLeft()
    {
        var game = await CreateGameAsync();
        await _client.PostAsJsonAsync($"/api/games/{game.Id}/doubles", new { playerId = 2 });

        var removed = await _client.DeleteAsync($"/api/games/{game.Id}/doubles/2");
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        Assert.Equal(0, (await removed.Content.ReadFromJsonAsync<GameDetailsDto>())!.Players.Single(p => p.PlayerId == 2).Doubles);

        var none = await _client.DeleteAsync($"/api/games/{game.Id}/doubles/2");
        Assert.Equal(HttpStatusCode.Conflict, none.StatusCode);
    }

    [Fact]
    public async Task PostDouble_RejectsAPlayerOutsideTheGame()
    {
        var game = await CreateGameAsync();

        var response = await _client.PostAsJsonAsync($"/api/games/{game.Id}/doubles", new { playerId = 3 });

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

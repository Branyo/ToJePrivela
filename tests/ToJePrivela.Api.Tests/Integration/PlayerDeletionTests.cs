using System.Net;
using System.Net.Http.Json;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Api.Tests.Integration;

/// <summary>Deleting a player keeps the history of their games: their seat becomes an unknown player.</summary>
public class PlayerDeletionTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public PlayerDeletionTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DeletePlayer_CancelsTheirRunningGameAndKeepsTheirSeatAsAnUnknownPlayer()
    {
        var leaving = await CreatePlayerAsync();
        var staying = await CreatePlayerAsync();
        var game = await CreateGameAsync(leaving.Id, staying.Id);
        var question = await CreateQuestionAsync();
        await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = leaving.Id, questionId = question.Id });

        var response = await _client.DeleteAsync($"/api/players/{leaving.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var details = (await _client.GetFromJsonAsync<GameDetailsDto>($"/api/games/{game.Id}/details"))!;
        Assert.True(details.Cancelled);
        Assert.NotNull(details.Finished);
        Assert.All(details.Players, player => Assert.False(player.IsLoser));
        var unknown = Assert.Single(details.Players, player => player.PlayerId is null);
        Assert.Null(unknown.Name);
        Assert.Null(unknown.Avatar);
        Assert.Equal((1, 3), (unknown.BadCards, unknown.BadPoints));
        Assert.Equal(staying.Id, Assert.Single(details.Players, player => player.PlayerId is not null).PlayerId);
    }

    [Fact]
    public async Task DeletePlayer_LeavesTheCancelledGameUnplayable()
    {
        var leaving = await CreatePlayerAsync();
        var staying = await CreatePlayerAsync();
        var game = await CreateGameAsync(leaving.Id, staying.Id);
        var question = await CreateQuestionAsync();

        await _client.DeleteAsync($"/api/players/{leaving.Id}");

        var card = await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = staying.Id, questionId = question.Id });
        var reopen = await _client.PutAsJsonAsync($"/api/games/{game.Id}", new { started = game.Started });
        Assert.Equal(HttpStatusCode.Conflict, card.StatusCode);
        Assert.Equal("Game.AlreadyFinished", await ProblemResponse.CodeOf(card));
        Assert.Equal("Game.CannotReopen", await ProblemResponse.CodeOf(reopen));
    }

    [Fact]
    public async Task DeletePlayer_KeepsTheResultOfAFinishedGame()
    {
        var leaving = await CreatePlayerAsync();
        var staying = await CreatePlayerAsync();
        var game = await CreateGameAsync(leaving.Id, staying.Id);
        var question = await CreateQuestionAsync();
        await _client.PostAsJsonAsync($"/api/games/{game.Id}/bad-cards", new { playerId = leaving.Id, questionId = question.Id });
        var finished = (await (await _client.PostAsync($"/api/games/{game.Id}/finish", null))
            .Content.ReadFromJsonAsync<GameDetailsDto>())!.Finished;

        await _client.DeleteAsync($"/api/players/{leaving.Id}");

        var details = (await _client.GetFromJsonAsync<GameDetailsDto>($"/api/games/{game.Id}/details"))!;
        Assert.False(details.Cancelled);
        Assert.Equal(finished, details.Finished);
        Assert.True(Assert.Single(details.Players, player => player.PlayerId is null).IsLoser);
    }

    [Fact]
    public async Task DeletePlayer_CanLeaveSeveralUnknownPlayersInOneGame()
    {
        var first = await CreatePlayerAsync();
        var second = await CreatePlayerAsync();
        var third = await CreatePlayerAsync();
        var game = await CreateGameAsync(first.Id, second.Id, third.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/players/{first.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/players/{second.Id}")).StatusCode);

        var summary = (await _client.GetFromJsonAsync<GameDto>($"/api/games/{game.Id}"))!;
        Assert.Equal([third.Id, null, null], summary.PlayerIds);
        Assert.True(summary.Cancelled);
    }

    private async Task<PlayerDto> CreatePlayerAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/players", new { name = $"P{Guid.NewGuid():N}"[..20] });
        return (await response.Content.ReadFromJsonAsync<PlayerDto>())!;
    }

    private async Task<GameDto> CreateGameAsync(params int[] playerIds)
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { playerIds });
        return (await response.Content.ReadFromJsonAsync<GameDto>())!;
    }

    private async Task<QuestionDto> CreateQuestionAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/questions",
            new { text = $"How many cars in {Guid.NewGuid():N}?", answer = "42", categoryId = 1, badPoints = 3 });
        return (await response.Content.ReadFromJsonAsync<QuestionDto>())!;
    }
}

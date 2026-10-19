using System.Net;
using System.Net.Http.Json;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Questions.Dtos;
using ToJePrivela.Application.Statistics.Dtos;

namespace ToJePrivela.Api.Tests.Integration;

public class PlayerStatisticsEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;
    private readonly HttpClient _member;

    public PlayerStatisticsEndpointsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
        _member = factory.CreateClientAs(factory.Member);
    }

    [Fact]
    public async Task GetStatistics_CountsOnlyFinishedGames()
    {
        var loser = await CreatePlayerAsync();
        var other = await CreatePlayerAsync();
        var question = await CreateQuestionAsync();
        var finished = await CreateGameAsync(loser.Id, other.Id);
        await _client.PostAsJsonAsync($"/api/games/{finished.Id}/bad-cards", new { playerId = loser.Id, calledByPlayerId = loser.Id, questionId = question.Id });
        await _client.PostAsync($"/api/games/{finished.Id}/finish", null);
        var running = await CreateGameAsync(loser.Id, other.Id);
        await _client.PostAsJsonAsync($"/api/games/{running.Id}/bad-cards", new { playerId = loser.Id, calledByPlayerId = loser.Id, questionId = question.Id });

        var response = await _client.GetAsync($"/api/players/{loser.Id}/statistics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var statistics = (await response.Content.ReadFromJsonAsync<PlayerStatisticsDto>())!;
        Assert.Equal(loser.Id, statistics.Player.Id);
        Assert.Equal((1, 1, 3, 1), (statistics.GamesPlayed, statistics.BadCards, statistics.FinalBadPoints, statistics.Losses));
        Assert.Equal(2, statistics.LossIndex);
        Assert.Equal(finished.Id, statistics.WorstGame!.GameId);
        // The loser called the only round and took the card: a false alarm, and a loss crying wolf.
        Assert.Equal((1, 1, 0, 1), (statistics.GamesWithCalls, statistics.TooMuchCalls, statistics.CorrectCalls, statistics.FalseAlarms));
        Assert.Equal(0, statistics.CallAccuracy);
        Assert.Equal(1, statistics.FalseAlarmCardShare);
        Assert.Equal(1, statistics.CriedWolfLosses);
        Assert.Null(statistics.MostCorrectCalls);
    }

    [Fact]
    public async Task GetStatistics_WithoutGames_HasNothingToAverage()
    {
        var player = await CreatePlayerAsync();

        var statistics = (await _client.GetFromJsonAsync<PlayerStatisticsDto>($"/api/players/{player.Id}/statistics"))!;

        Assert.Equal(0, statistics.GamesPlayed);
        Assert.Null(statistics.AverageBadCards);
        Assert.Null(statistics.LossIndex);
        Assert.Null(statistics.BestGame);
    }

    [Fact]
    public async Task GetStatistics_ReturnsNotFoundForAnotherLoginsPlayer()
    {
        var player = await CreatePlayerAsync();

        var response = await _member.GetAsync($"/api/players/{player.Id}/statistics");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Player.NotFound", await ProblemResponse.CodeOf(response));
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
            new { textSk = $"Koľko áut je v {Guid.NewGuid():N}?", textEn = "How many cars are there?", answer = "42", categoryId = 1, badPoints = 3 });
        return (await response.Content.ReadFromJsonAsync<QuestionDto>())!;
    }
}

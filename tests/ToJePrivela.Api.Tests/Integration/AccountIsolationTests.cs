using System.Net;
using System.Net.Http.Json;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Players.Dtos;

namespace ToJePrivela.Api.Tests.Integration;

/// <summary>Every login has its own players and games; another login's ids are simply not found.</summary>
public class AccountIsolationTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _member;

    public AccountIsolationTests(ApiFactory factory)
    {
        _admin = factory.CreateClient();
        _member = factory.CreateClientAs(factory.Member);
    }

    [Fact]
    public async Task Players_AreListedOnlyForTheirOwnLogin()
    {
        var mine = await CreatePlayerAsync(_member, Unique("Mine"));

        var memberPlayers = await _member.GetFromJsonAsync<List<PlayerDto>>("/api/players");
        var adminPlayers = await _admin.GetFromJsonAsync<List<PlayerDto>>("/api/players");

        Assert.Contains(memberPlayers!, p => p.Id == mine.Id);
        Assert.DoesNotContain(memberPlayers!, p => p.Name == "Admin");
        Assert.DoesNotContain(adminPlayers!, p => p.Id == mine.Id);
    }

    [Fact]
    public async Task APlayerOfAnotherLogin_IsNotFound()
    {
        var get = await _member.GetAsync("/api/players/1");
        var rename = await _member.PutAsJsonAsync("/api/players/1", new { name = "Hijacked" });
        var delete = await _member.DeleteAsync("/api/players/1");

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, rename.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _admin.GetAsync("/api/players/1")).StatusCode);
    }

    [Fact]
    public async Task AName_CanBeUsedOnceUnderEveryLogin()
    {
        var name = Unique("Twin");

        var mine = await _member.PostAsJsonAsync("/api/players", new { name });
        var theirs = await _admin.PostAsJsonAsync("/api/players", new { name });
        var again = await _member.PostAsJsonAsync("/api/players", new { name = name.ToUpperInvariant() });

        Assert.Equal(HttpStatusCode.Created, mine.StatusCode);
        Assert.Equal(HttpStatusCode.Created, theirs.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("Player.NameTaken", await ProblemResponse.CodeOf(again));
    }

    [Fact]
    public async Task AGame_CannotSeatThePlayersOfAnotherLogin()
    {
        var mine = await CreatePlayerAsync(_member, Unique("Seat"));

        var response = await _member.PostAsJsonAsync("/api/games", new { playerIds = new[] { mine.Id, 1 } });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Game.UnknownPlayers", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task Games_AreSavedUnderTheLoginThatPlayedThem()
    {
        var first = await CreatePlayerAsync(_member, Unique("One"));
        var second = await CreatePlayerAsync(_member, Unique("Two"));
        var game = await (await _member.PostAsJsonAsync("/api/games", new { playerIds = new[] { first.Id, second.Id } }))
            .Content.ReadFromJsonAsync<GameDto>();

        var memberGames = await _member.GetFromJsonAsync<List<GameDto>>("/api/games");
        var adminGames = await _admin.GetFromJsonAsync<List<GameDto>>("/api/games");

        Assert.Contains(memberGames!, g => g.Id == game!.Id);
        Assert.DoesNotContain(adminGames!, g => g.Id == game!.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.GetAsync($"/api/games/{game!.Id}/details")).StatusCode);
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _admin.PostAsJsonAsync($"/api/games/{game.Id}/doubles", new { playerId = first.Id })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.PostAsync($"/api/games/{game.Id}/finish", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.DeleteAsync($"/api/games/{game.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _member.GetAsync($"/api/games/{game.Id}/details")).StatusCode);
    }

    private static async Task<PlayerDto> CreatePlayerAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/players", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PlayerDto>())!;
    }

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..16];
}

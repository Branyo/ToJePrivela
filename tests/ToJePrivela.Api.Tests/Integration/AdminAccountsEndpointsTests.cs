using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using ToJePrivela.Application.Accounts.Dtos;
using ToJePrivela.Application.Administration.Dtos;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Statistics.Dtos;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;

namespace ToJePrivela.Api.Tests.Integration;

/// <summary>Admins see every login with its players, games and statistics, and may delete a login with all it owns.</summary>
public class AdminAccountsEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _admin;

    public AdminAccountsEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _admin = factory.CreateClient();
    }

    [Theory]
    [InlineData("GET", "/api/admin/accounts")]
    [InlineData("GET", "/api/admin/accounts/1")]
    [InlineData("GET", "/api/admin/accounts/1/players")]
    [InlineData("GET", "/api/admin/accounts/1/players/1/statistics")]
    [InlineData("GET", "/api/admin/accounts/1/games/history")]
    [InlineData("GET", "/api/admin/accounts/1/games/1/details")]
    [InlineData("DELETE", "/api/admin/accounts/1")]
    public async Task EveryEndpoint_IsForAdminsOnly(string method, string url)
    {
        var member = _factory.CreateClientAs(_factory.Member);

        var response = await member.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Auth.Forbidden", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task GetAccounts_ListsEveryLoginTheMostRecentlySignedInFirst()
    {
        var login = await CreateLoginAsync();

        var accounts = (await _admin.GetFromJsonAsync<List<AccountOverviewDto>>("/api/admin/accounts"))!;

        Assert.Equal(login.Account.Id, accounts[0].Id);
        Assert.Contains(accounts, a => a.Id == _factory.Admin.Id && a.IsAdmin);
        Assert.Contains(accounts, a => a.Id == _factory.Member.Id && !a.IsAdmin);
        var signedIn = accounts.Where(a => a.LastSignedInAt is not null).Select(a => a.LastSignedInAt).ToList();
        Assert.Equal(signedIn.OrderByDescending(at => at), signedIn);
        Assert.NotNull(accounts[0].LastSignedInAt);
    }

    [Fact]
    public async Task GetAccounts_OffersToDeleteAllButTheAdminsOwnAndTheReservedLogin()
    {
        var otherAdmin = _factory.CreateClientAs(await AddAdminAsync());

        var accounts = (await otherAdmin.GetFromJsonAsync<List<AccountOverviewDto>>("/api/admin/accounts"))!;

        Assert.False(accounts.Single(a => a.Id == _factory.Admin.Id).CanDelete);
        Assert.True(accounts.Single(a => a.Id == _factory.Member.Id).CanDelete);
        Assert.Equal(2, accounts.Count(a => !a.CanDelete));
    }

    [Fact]
    public async Task GetAccount_ReadsOneLogin()
    {
        var login = await CreateLoginAsync();

        var account = await _admin.GetFromJsonAsync<AccountOverviewDto>($"/api/admin/accounts/{login.Account.Id}");
        var missing = await _admin.GetAsync($"/api/admin/accounts/{int.MaxValue}");

        Assert.Equal((login.Account.Name, false, true), (account!.Name, account.IsAdmin, account.CanDelete));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal("Account.NotFound", await ProblemResponse.CodeOf(missing));
    }

    [Fact]
    public async Task ALoginsPlayersGamesAndStatistics_ReadAsTheLoginReadsThem()
    {
        var login = await CreateLoginAsync();
        var (first, second, game) = await PlayAFinishedGameAsync(login.Client);
        var root = $"/api/admin/accounts/{login.Account.Id}";

        var players = await _admin.GetFromJsonAsync<List<PlayerDto>>($"{root}/players");
        var history = await _admin.GetFromJsonAsync<GameHistoryDto>($"{root}/games/history?status=Finished");
        var details = await _admin.GetFromJsonAsync<GameDetailsDto>($"{root}/games/{game.Id}/details");
        var statistics = await _admin.GetFromJsonAsync<PlayerStatisticsDto>($"{root}/players/{second.Id}/statistics");

        Assert.Equal(new[] { first.Id, second.Id }, players!.Select(p => p.Id).Order());
        Assert.Equal(game.Id, Assert.Single(history!.Games).Id);
        Assert.Equal(
            await login.Client.GetStringAsync($"/api/games/{game.Id}/details"),
            await _admin.GetStringAsync($"{root}/games/{game.Id}/details"));
        Assert.True(details!.Players.Single(p => p.PlayerId == second.Id).IsLoser);
        Assert.Equal((1, 1), (statistics!.GamesPlayed, statistics.Losses));
    }

    [Fact]
    public async Task Reads_DoNotReachTheDataOfAnotherLogin()
    {
        var login = await CreateLoginAsync();
        var other = await CreateLoginAsync();
        var (player, _, game) = await PlayAFinishedGameAsync(other.Client);
        var root = $"/api/admin/accounts/{login.Account.Id}";

        var details = await _admin.GetAsync($"{root}/games/{game.Id}/details");
        var statistics = await _admin.GetAsync($"{root}/players/{player.Id}/statistics");

        Assert.Equal("Game.NotFound", await ProblemResponse.CodeOf(details));
        Assert.Equal("Player.NotFound", await ProblemResponse.CodeOf(statistics));
        Assert.Empty((await _admin.GetFromJsonAsync<GameHistoryDto>($"{root}/games/history"))!.Games);
    }

    [Theory]
    [InlineData("/players")]
    [InlineData("/players/1/statistics")]
    [InlineData("/games/history")]
    [InlineData("/games/1/details")]
    public async Task Reads_OfAnUnknownLogin_AreNotFound(string path)
    {
        var response = await _admin.GetAsync($"/api/admin/accounts/{int.MaxValue}{path}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Account.NotFound", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task GetGameHistory_ValidatesTheFilterLikeTheLoginsOwnHistory()
    {
        var response = await _admin.GetAsync($"/api/admin/accounts/{_factory.Member.Id}/games/history?take=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_DeletesTheLoginWithItsPlayersAndGamesAndEndsItsSignIn()
    {
        var login = await CreateLoginAsync();
        var (_, _, game) = await PlayAFinishedGameAsync(login.Client);
        var otherPlayers = await _admin.GetFromJsonAsync<List<PlayerDto>>("/api/players");

        var response = await _admin.DeleteAsync($"/api/admin/accounts/{login.Account.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.DoesNotContain(
            (await _admin.GetFromJsonAsync<List<AccountOverviewDto>>("/api/admin/accounts"))!,
            a => a.Id == login.Account.Id);
        var signedOut = await login.Client.GetAsync($"/api/games/{game.Id}/details");
        Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
        Assert.Equal("Auth.UnknownAccount", await ProblemResponse.CodeOf(signedOut));
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.GetAsync($"/api/admin/accounts/{login.Account.Id}/players")).StatusCode);
        Assert.Equal(otherPlayers, await _admin.GetFromJsonAsync<List<PlayerDto>>("/api/players"));
        Assert.Equal(HttpStatusCode.NotFound, (await _admin.DeleteAsync($"/api/admin/accounts/{login.Account.Id}")).StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_RefusesTheAdminsOwnLogin()
    {
        var response = await _admin.DeleteAsync($"/api/admin/accounts/{_factory.Admin.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Account.CannotDeleteOwn", await ProblemResponse.CodeOf(response));
        Assert.Equal(HttpStatusCode.OK, (await _admin.GetAsync("/api/auth/me")).StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_RefusesTheReservedLoginEvenToAnotherAdmin()
    {
        var otherAdmin = _factory.CreateClientAs(await AddAdminAsync());

        var response = await otherAdmin.DeleteAsync($"/api/admin/accounts/{_factory.Admin.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Account.CannotDeleteReserved", await ProblemResponse.CodeOf(response));
        Assert.Equal(HttpStatusCode.OK, (await _admin.GetAsync("/api/auth/me")).StatusCode);
    }

    /// <summary>A second admin, as one more name in <c>Authentication:Admins</c> would add.</summary>
    private async Task<Account> AddAdminAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ToJePrivelaDbContext>();
        var admin = Account.CreateAdmin($"A-{Guid.NewGuid():N}"[..12], "hash", DateTime.UtcNow);
        context.Accounts.Add(admin);
        await context.SaveChangesAsync();
        return admin;
    }

    /// <summary>A new login, signed in: it becomes the most recently signed in.</summary>
    private async Task<(HttpClient Client, AccountDto Account)> CreateLoginAsync()
    {
        var name = $"L-{Guid.NewGuid():N}"[..12];
        var response = await _factory.CreateAnonymousClient()
            .PostAsJsonAsync("/api/auth/accounts", new { name, password = "secret-password" });
        response.EnsureSuccessStatusCode();
        var signedIn = (await response.Content.ReadFromJsonAsync<SignedInDto>())!;

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signedIn.AccessToken);
        return (client, signedIn.Account);
    }

    /// <summary>Two new players of the login and a finished game the second one lost with a chooser card.</summary>
    private static async Task<(PlayerDto First, PlayerDto Second, GameDto Game)> PlayAFinishedGameAsync(HttpClient client)
    {
        var first = await CreatePlayerAsync(client, "Anna");
        var second = await CreatePlayerAsync(client, "Boris");
        var created = await client.PostAsJsonAsync(
            "/api/games",
            new { playerIds = new[] { first.Id, second.Id }, badPointsMode = "Chooser" });
        created.EnsureSuccessStatusCode();
        var game = (await created.Content.ReadFromJsonAsync<GameDto>())!;

        (await client.PostAsJsonAsync(
            $"/api/games/{game.Id}/bad-cards",
            new { playerId = second.Id, calledByPlayerId = first.Id, questionId = 1, badPoints = 3 })).EnsureSuccessStatusCode();
        (await client.PostAsync($"/api/games/{game.Id}/finish", null)).EnsureSuccessStatusCode();

        return (first, second, game);
    }

    private static async Task<PlayerDto> CreatePlayerAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/players", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PlayerDto>())!;
    }
}

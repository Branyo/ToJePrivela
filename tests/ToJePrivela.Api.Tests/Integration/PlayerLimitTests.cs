using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ToJePrivela.Application.Accounts.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Api.Tests.Integration;

public class PlayerLimitTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public PlayerLimitTests(ApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ALogin_KeepsAtMostTheMaximumNumberOfPlayers()
    {
        var client = await NewLoginAsync();

        for (var number = 1; number <= Account.MaxPlayers; number++)
        {
            var created = await client.PostAsJsonAsync("/api/players", new { name = $"Player {number}" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        }

        var tooMany = await client.PostAsJsonAsync("/api/players", new { name = "One too many" });

        Assert.Equal(HttpStatusCode.Conflict, tooMany.StatusCode);
        Assert.Equal("Player.LimitReached", await ProblemResponse.CodeOf(tooMany));

        // Deleting one makes room again, and other logins were never affected.
        var players = await client.GetFromJsonAsync<List<IdOnly>>("/api/players");
        await client.DeleteAsync($"/api/players/{players![0].Id}");
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/players", new { name = "One too many" })).StatusCode);
        Assert.Equal(
            HttpStatusCode.Created,
            (await _factory.CreateClientAs(_factory.Member).PostAsJsonAsync("/api/players", new { name = $"M{Guid.NewGuid():N}"[..12] })).StatusCode);
    }

    private async Task<HttpClient> NewLoginAsync()
    {
        var created = await _factory.CreateAnonymousClient().PostAsJsonAsync(
            "/api/auth/accounts",
            new { name = $"l{Guid.NewGuid():N}"[..20], password = "limit-password" });
        var signedIn = await created.Content.ReadFromJsonAsync<SignedInDto>();

        var client = _factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signedIn!.AccessToken);
        return client;
    }

    private sealed record IdOnly(int Id);
}

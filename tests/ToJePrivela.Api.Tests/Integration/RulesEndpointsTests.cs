using System.Net.Http.Json;
using ToJePrivela.Application.Rules.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Api.Tests.Integration;

public class RulesEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client;

    public RulesEndpointsTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetRules_ServesTheEnforcedLimits()
    {
        var rules = await _client.GetFromJsonAsync<GameRulesDto>("/api/rules");

        Assert.Equal(new LimitDto(Game.MinPlayers, Game.MaxPlayers), rules!.Players);
        Assert.Equal(new LimitDto(Question.MinBadPoints, Question.MaxBadPoints), rules.BadPoints);
        Assert.Equal(PlayerAvatars.All, rules.Avatars);
    }
}

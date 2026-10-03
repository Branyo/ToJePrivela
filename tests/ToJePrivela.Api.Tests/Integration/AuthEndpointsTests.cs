using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NSubstitute;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Accounts.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Api.Tests.Integration;

public class AuthEndpointsTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _anonymous;

    public AuthEndpointsTests(ApiFactory factory)
    {
        _factory = factory;
        _anonymous = factory.CreateAnonymousClient();
    }

    [Fact]
    public async Task GetProviders_ListsTheConfiguredProvidersWithoutSigningIn()
    {
        var providers = await _anonymous.GetFromJsonAsync<List<SignInProviderDto>>("/api/auth/providers");

        Assert.Equal([new SignInProviderDto("Google", ApiFactory.GoogleClientId)], providers);
    }

    [Fact]
    public async Task SignIn_ReturnsAnAccessTokenThatOpensTheApi()
    {
        var externalId = $"google-{Guid.NewGuid():N}";
        _factory.GoogleVerifier.VerifyAsync("valid-token", Arg.Any<CancellationToken>())
            .Returns(new ExternalIdentity(IdentityProvider.Google, externalId, "new@example.com", "Newcomer"));

        var response = await _anonymous.PostAsJsonAsync("/api/auth/sign-in", new { provider = "Google", token = "valid-token" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var signedIn = await response.Content.ReadFromJsonAsync<SignedInDto>();
        Assert.Equal("Newcomer", signedIn!.Account.DisplayName);
        Assert.False(signedIn.Account.IsAdmin);

        using var client = _factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signedIn.AccessToken);
        var me = await client.GetFromJsonAsync<AccountDto>("/api/auth/me");
        Assert.Equal(signedIn.Account.Id, me!.Id);
    }

    [Fact]
    public async Task SignIn_AnswersAnInvalidTokenWith401()
    {
        _factory.GoogleVerifier.VerifyAsync("forged", Arg.Any<CancellationToken>()).Returns((ExternalIdentity?)null);

        var response = await _anonymous.PostAsJsonAsync("/api/auth/sign-in", new { provider = "Google", token = "forged" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Auth.InvalidToken", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task SignIn_AnswersAProviderThatIsNotConfiguredWith400()
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/sign-in", new { provider = "Facebook", token = "fb" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Auth.ProviderNotConfigured", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task Me_DescribesTheSignedInAdmin()
    {
        var me = await _factory.CreateClient().GetFromJsonAsync<AccountDto>("/api/auth/me");

        Assert.Equal(_factory.Admin.Id, me!.Id);
        Assert.True(me.IsAdmin);
    }

    [Fact]
    public async Task Me_AnswersWithoutATokenWith401()
    {
        var response = await _anonymous.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Auth.Unauthenticated", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task Me_RejectsATamperedToken()
    {
        using var client = _factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _factory.TokenFor(_factory.Member) + "x");

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

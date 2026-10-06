using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Accounts.Dtos;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Infrastructure.Persistence;

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
    public async Task SignIn_ReturnsAnAccessTokenThatOpensTheApi()
    {
        var response = await _anonymous.PostAsJsonAsync(
            "/api/auth/sign-in",
            new { name = "test MEMBER", password = ApiFactory.MemberPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var signedIn = await response.Content.ReadFromJsonAsync<SignedInDto>();
        Assert.Equal(new AccountDto(_factory.Member.Id, "Test member", false), signedIn!.Account);
        Assert.True(signedIn.ExpiresAt > DateTime.UtcNow);

        var me = await WithToken(signedIn.AccessToken).GetFromJsonAsync<AccountDto>("/api/auth/me");
        Assert.Equal(signedIn.Account, me);
    }

    [Fact]
    public async Task SignIn_AnswersAWrongPasswordWith401()
    {
        var response = await _anonymous.PostAsJsonAsync(
            "/api/auth/sign-in",
            new { name = "Test member", password = "not-the-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Auth.WrongPassword", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task SignIn_AnswersAnUnknownNameWith404SoTheClientCanOfferToCreateIt()
    {
        var response = await _anonymous.PostAsJsonAsync(
            "/api/auth/sign-in",
            new { name = $"nobody-{Guid.NewGuid():N}"[..20], password = "whatever-password" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Auth.UnknownLogin", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task SignIn_WithOnlyAName_SaysWhetherTheLoginExists()
    {
        var unknown = await _anonymous.PostAsJsonAsync("/api/auth/sign-in", new { name = NewName(), password = "" });
        var known = await _anonymous.PostAsJsonAsync("/api/auth/sign-in", new { name = "Test member" });

        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, known.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_CreatesALoginThatCanSignInAgain()
    {
        var name = NewName();

        var created = await _anonymous.PostAsJsonAsync("/api/auth/accounts", new { name, password = "new-password" });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var signedIn = await created.Content.ReadFromJsonAsync<SignedInDto>();
        Assert.Equal(name, signedIn!.Account.Name);
        Assert.False(signedIn.Account.IsAdmin);
        Assert.Equal(HttpStatusCode.OK, (await WithToken(signedIn.AccessToken).GetAsync("/api/auth/me")).StatusCode);

        var again = await _anonymous.PostAsJsonAsync("/api/auth/sign-in", new { name, password = "new-password" });
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_StoresAHashNotThePassword()
    {
        var name = NewName();

        await _anonymous.PostAsJsonAsync("/api/auth/accounts", new { name, password = "plain-text-password" });

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ToJePrivelaDbContext>();
        var stored = await context.Accounts.SingleAsync(a => a.Name == name);
        Assert.DoesNotContain("plain-text-password", stored.PasswordHash);
    }

    [Fact]
    public async Task CreateAccount_AnswersATakenNameWith409()
    {
        var response = await _anonymous.PostAsJsonAsync(
            "/api/auth/accounts",
            new { name = "TEST member", password = "other-password" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Auth.NameTaken", await ProblemResponse.CodeOf(response));
    }

    [Theory]
    [InlineData("ab", "long-enough-password")]
    [InlineData("Valid name", "short")]
    public async Task CreateAccount_AnswersAnInvalidNameOrPasswordWith400(string name, string password)
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/accounts", new { name, password });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateAccount_NeverCreatesAnAdmin()
    {
        var response = await _anonymous.PostAsJsonAsync(
            "/api/auth/accounts",
            new { name = NewName(), password = "new-password", isAdmin = true });

        Assert.False((await response.Content.ReadFromJsonAsync<SignedInDto>())!.Account.IsAdmin);
    }

    [Fact]
    public async Task Me_WithoutAToken_Answers401()
    {
        var response = await _anonymous.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Auth.Unauthenticated", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task ATokenForALoginThatIsGone_Answers401()
    {
        // Never stored, so its id (0) names no login; as after the database was reset.
        var token = _factory.TokenFor(Account.Register("Ghost", "hash", DateTime.UtcNow));

        var response = await WithToken(token).GetAsync("/api/players");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Auth.UnknownAccount", await ProblemResponse.CodeOf(response));
    }

    [Fact]
    public async Task RevokingAdminRights_CountsAtOnceForATokenAlreadyIssued()
    {
        Account admin;

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ToJePrivelaDbContext>();
            admin = Account.CreateAdmin(NewName(), "hash", DateTime.UtcNow);
            context.Accounts.Add(admin);
            await context.SaveChangesAsync();
        }

        var client = _factory.CreateClientAs(admin);
        Assert.True((await client.GetFromJsonAsync<AccountDto>("/api/auth/me"))!.IsAdmin);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ToJePrivelaDbContext>();
            (await context.Accounts.SingleAsync(a => a.Id == admin.Id)).RevokeAdmin();
            await context.SaveChangesAsync();
        }

        var response = await client.DeleteAsync("/api/question-categories/999999");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ATamperedToken_Answers401()
    {
        var token = _factory.TokenFor(_factory.Member);

        var response = await WithToken(token[..^4] + "AAAA").GetAsync("/api/players");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient WithToken(string token)
    {
        var client = _factory.CreateAnonymousClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string NewName() => $"u{Guid.NewGuid():N}"[..20];
}

public sealed class TwoSignInPermitsApiFactory : ApiFactory
{
    protected override int SignInPermitLimit => 2;
}

public class SignInRateLimitTests : IClassFixture<TwoSignInPermitsApiFactory>
{
    private readonly HttpClient _anonymous;

    public SignInRateLimitTests(TwoSignInPermitsApiFactory factory)
    {
        _anonymous = factory.CreateAnonymousClient();
    }

    [Fact]
    public async Task GuessingPasswordsQuickly_IsStoppedWith429()
    {
        var attempt = new { name = "Test member", password = "guess-password" };

        await _anonymous.PostAsJsonAsync("/api/auth/sign-in", attempt);
        await _anonymous.PostAsJsonAsync("/api/auth/accounts", attempt);
        var third = await _anonymous.PostAsJsonAsync("/api/auth/sign-in", attempt);

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task OtherLoginNamesFromTheSameAddress_HaveTheirOwnAttempts()
    {
        var name = UniqueName();
        await _anonymous.PostAsJsonAsync("/api/auth/sign-in", new { name, password = "guess-password" });
        await _anonymous.PostAsJsonAsync("/api/auth/sign-in", new { name, password = "guess-password" });

        var otherName = await _anonymous.PostAsJsonAsync(
            "/api/auth/sign-in",
            new { name = UniqueName(), password = "guess-password" });

        Assert.Equal(HttpStatusCode.NotFound, otherName.StatusCode);
    }

    [Fact]
    public async Task OneNameSpelledDifferently_SharesItsAttempts()
    {
        var name = UniqueName();
        await _anonymous.PostAsJsonAsync("/api/auth/sign-in", new { name, password = "guess-password" });
        await _anonymous.PostAsJsonAsync("/api/auth/sign-in", new { name = $"  {name.ToUpperInvariant()} ", password = "x" });

        var third = await _anonymous.PostAsJsonAsync("/api/auth/sign-in", new { Name = name, password = "guess-password" });

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task ARefusal_SaysHowLongToWait()
    {
        var attempt = new { name = UniqueName(), password = "guess-password" };
        await _anonymous.PostAsJsonAsync("/api/auth/sign-in", attempt);
        await _anonymous.PostAsJsonAsync("/api/auth/sign-in", attempt);

        var refused = await _anonymous.PostAsJsonAsync("/api/auth/sign-in", attempt);

        Assert.Equal(HttpStatusCode.TooManyRequests, refused.StatusCode);
        Assert.Equal("application/problem+json", refused.Content.Headers.ContentType?.MediaType);
        var problem = await refused.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(RateLimitRejection.Code, problem.GetProperty("code").GetString());
        var seconds = problem.GetProperty(RateLimitRejection.RetryAfterSecondsExtension).GetInt32();
        Assert.InRange(seconds, 1, 60);
        Assert.Equal(TimeSpan.FromSeconds(seconds), refused.Headers.RetryAfter?.Delta);
    }

    private static string UniqueName() => $"u{Guid.NewGuid():N}"[..20];
}

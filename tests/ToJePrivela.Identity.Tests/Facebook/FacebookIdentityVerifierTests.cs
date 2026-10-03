using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Identity.Facebook;

namespace ToJePrivela.Identity.Tests.Facebook;

public class FacebookIdentityVerifierTests
{
    private const string AppId = "app-1";
    private const string AppSecret = "secret";
    private const string Graph = "https://graph.test/v23.0";

    [Fact]
    public async Task VerifyAsync_ReturnsTheIdentityOfATokenIssuedToThisApp()
    {
        var handler = GraphAnswering(
            debugToken: """{ "data": { "app_id": "app-1", "is_valid": true, "user_id": "42" } }""",
            me: """{ "id": "42", "name": "Duri", "email": "duri@example.com" }""");

        var identity = await Verifier(handler).VerifyAsync("user token");

        Assert.Equal(new ExternalIdentity(IdentityProvider.Facebook, "42", "duri@example.com", "Duri"), identity);
    }

    [Fact]
    public async Task VerifyAsync_AsksDebugTokenWithTheAppTokenAndSignsTheProfileRequest()
    {
        var requests = new List<Uri>();
        var handler = GraphAnswering(
            debugToken: """{ "data": { "app_id": "app-1", "is_valid": true, "user_id": "42" } }""",
            me: """{ "id": "42", "name": "Duri" }""",
            requests);

        await Verifier(handler).VerifyAsync("user token");

        Assert.Equal(
            $"{Graph}/debug_token?input_token=user%20token&access_token=app-1%7Csecret",
            requests[0].AbsoluteUri);
        var proof = Convert.ToHexStringLower(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(AppSecret), Encoding.UTF8.GetBytes("user token")));
        Assert.Equal(
            $"{Graph}/me?fields=id,name,email&access_token=user%20token&appsecret_proof={proof}",
            requests[1].AbsoluteUri);
    }

    [Fact]
    public async Task VerifyAsync_RejectsATokenIssuedToAnotherApp()
    {
        var handler = GraphAnswering(
            debugToken: """{ "data": { "app_id": "someone-elses-app", "is_valid": true, "user_id": "42" } }""",
            me: """{ "id": "42", "name": "Duri" }""");

        Assert.Null(await Verifier(handler).VerifyAsync("user token"));
    }

    [Fact]
    public async Task VerifyAsync_RejectsAnInvalidToken()
    {
        var handler = GraphAnswering(
            debugToken: """{ "data": { "app_id": "app-1", "is_valid": false } }""",
            me: "{}");

        Assert.Null(await Verifier(handler).VerifyAsync("expired"));
    }

    [Fact]
    public async Task VerifyAsync_RejectsATokenFacebookRefuses()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{ "error": { "code": 190 } }""")
        });

        Assert.Null(await Verifier(handler).VerifyAsync("garbage"));
    }

    [Fact]
    public async Task VerifyAsync_ReportsAFacebookOutageAsUnavailable()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        await Assert.ThrowsAsync<IdentityProviderUnavailableException>(() => Verifier(handler).VerifyAsync("token"));
    }

    [Fact]
    public async Task VerifyAsync_ReportsAnUnreachableFacebookAsUnavailable()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("offline"));

        await Assert.ThrowsAsync<IdentityProviderUnavailableException>(() => Verifier(handler).VerifyAsync("token"));
    }

    [Fact]
    public async Task VerifyAsync_RejectsEverythingWithoutTheAppSecret()
    {
        var handler = new StubHttpMessageHandler(_ => throw new InvalidOperationException("must not be called"));
        var verifier = Verifier(handler, appSecret: "");

        Assert.Null(verifier.ClientId);
        Assert.Null(await verifier.VerifyAsync("token"));
    }

    private static StubHttpMessageHandler GraphAnswering(string debugToken, string me, List<Uri>? requests = null) =>
        new(request =>
        {
            requests?.Add(request.RequestUri!);
            var body = request.RequestUri!.AbsolutePath.EndsWith("/debug_token") ? debugToken : me;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        });

    private static FacebookIdentityVerifier Verifier(StubHttpMessageHandler handler, string appSecret = AppSecret) =>
        new(
            new HttpClient(handler),
            Options.Create(new FacebookOptions { AppId = AppId, AppSecret = appSecret, GraphApiUrl = Graph }),
            NullLogger<FacebookIdentityVerifier>.Instance);
}

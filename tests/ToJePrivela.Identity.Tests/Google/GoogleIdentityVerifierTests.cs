using Google.Apis.Auth;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Identity.Google;

namespace ToJePrivela.Identity.Tests.Google;

public class GoogleIdentityVerifierTests
{
    private const string ClientId = "client.apps.googleusercontent.com";

    private readonly IGoogleIdTokenValidator _validator = Substitute.For<IGoogleIdTokenValidator>();

    [Fact]
    public async Task VerifyAsync_ReturnsTheIdentityOfAValidToken()
    {
        _validator.ValidateAsync("id-token", ClientId).Returns(new GoogleJsonWebSignature.Payload
        {
            Subject = "sub-1",
            Email = "brano@example.com",
            EmailVerified = true,
            Name = "Brano"
        });

        var identity = await Verifier().VerifyAsync("id-token");

        Assert.Equal(new ExternalIdentity(IdentityProvider.Google, "sub-1", "brano@example.com", "Brano"), identity);
    }

    [Fact]
    public async Task VerifyAsync_DropsAnEmailGoogleDidNotVerify()
    {
        _validator.ValidateAsync("id-token", ClientId).Returns(new GoogleJsonWebSignature.Payload
        {
            Subject = "sub-1",
            Email = "admin@example.com",
            EmailVerified = false,
            Name = "Someone"
        });

        var identity = await Verifier().VerifyAsync("id-token");

        Assert.Null(identity!.Email);
    }

    [Fact]
    public async Task VerifyAsync_RejectsAnInvalidToken()
    {
        _validator.ValidateAsync("forged", ClientId).ThrowsAsync(new InvalidJwtException("JWT invalid"));

        Assert.Null(await Verifier().VerifyAsync("forged"));
    }

    [Fact]
    public async Task VerifyAsync_ReportsUnreachableGoogleAsUnavailable()
    {
        _validator.ValidateAsync("id-token", ClientId).ThrowsAsync(new HttpRequestException("offline"));

        await Assert.ThrowsAsync<IdentityProviderUnavailableException>(() => Verifier().VerifyAsync("id-token"));
    }

    [Fact]
    public async Task VerifyAsync_RejectsEverythingWhenNotConfigured()
    {
        var verifier = Verifier(clientId: " ");

        Assert.Null(verifier.ClientId);
        Assert.Null(await verifier.VerifyAsync("id-token"));
        await _validator.DidNotReceive().ValidateAsync(Arg.Any<string>(), Arg.Any<string>());
    }

    private GoogleIdentityVerifier Verifier(string clientId = ClientId) =>
        new(_validator, Options.Create(new GoogleOptions { ClientId = clientId }), NullLogger<GoogleIdentityVerifier>.Instance);
}

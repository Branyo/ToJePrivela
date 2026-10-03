using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Domain.Entities;
using ToJePrivela.Identity.Facebook.Contracts;

namespace ToJePrivela.Identity.Facebook;

/// <summary>
/// Verifies the user access token the Facebook JS SDK hands the browser. <c>debug_token</c>, asked with the app's own
/// token, proves the user token was issued to <em>this</em> app (a token from any other app is refused); <c>/me</c>
/// then reads the profile, signed with <c>appsecret_proof</c>.
/// </summary>
public sealed class FacebookIdentityVerifier : IExternalIdentityVerifier
{
    private readonly HttpClient _httpClient;
    private readonly FacebookOptions _options;
    private readonly ILogger<FacebookIdentityVerifier> _logger;

    public FacebookIdentityVerifier(
        HttpClient httpClient,
        IOptions<FacebookOptions> options,
        ILogger<FacebookIdentityVerifier> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public IdentityProvider Provider => IdentityProvider.Facebook;

    public string? ClientId => _options.IsConfigured ? _options.AppId!.Trim() : null;

    public async Task<ExternalIdentity?> VerifyAsync(string token, CancellationToken cancellationToken = default)
    {
        if (ClientId is not { } appId || string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var appToken = $"{appId}|{_options.AppSecret!.Trim()}";
        var debug = await GetAsync<DebugTokenResponse>(
            $"debug_token?input_token={Escape(token)}&access_token={Escape(appToken)}",
            cancellationToken);

        if (debug?.Data is not { IsValid: true, UserId: { Length: > 0 } userId } data || data.AppId != appId)
        {
            _logger.LogInformation("Facebook did not confirm an access token for this app.");
            return null;
        }

        var me = await GetAsync<MeResponse>(
            $"me?fields=id,name,email&access_token={Escape(token)}&appsecret_proof={AppSecretProof(token)}",
            cancellationToken);

        if (me?.Id != userId)
        {
            return null;
        }

        return new ExternalIdentity(IdentityProvider.Facebook, userId, me.Email, me.Name ?? me.Email ?? string.Empty);
    }

    /// <returns>The parsed answer, or null when Facebook refused the request (an invalid or expired token).</returns>
    private async Task<T?> GetAsync<T>(string relativeUrl, CancellationToken cancellationToken) where T : class
    {
        HttpResponseMessage response;

        try
        {
            response = await _httpClient.GetAsync($"{_options.GraphApiUrl.TrimEnd('/')}/{relativeUrl}", cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException
                                          && !cancellationToken.IsCancellationRequested)
        {
            throw new IdentityProviderUnavailableException("Facebook could not be reached.", exception);
        }

        using (response)
        {
            if ((int)response.StatusCode >= 500)
            {
                throw new IdentityProviderUnavailableException(
                    $"Facebook answered {(int)response.StatusCode}.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            try
            {
                return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            }
            catch (JsonException exception)
            {
                throw new IdentityProviderUnavailableException("Facebook answered with something unreadable.", exception);
            }
        }
    }

    private string AppSecretProof(string token)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(_options.AppSecret!.Trim()), Encoding.UTF8.GetBytes(token));
        return Convert.ToHexStringLower(hash);
    }

    private static string Escape(string value) => Uri.EscapeDataString(value);
}

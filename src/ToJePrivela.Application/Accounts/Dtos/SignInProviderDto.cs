namespace ToJePrivela.Application.Accounts.Dtos;

/// <param name="ClientId">What the provider's browser SDK is initialised with (Google client id, Facebook app id).</param>
public sealed record SignInProviderDto(string Provider, string ClientId);

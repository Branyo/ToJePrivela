namespace ToJePrivela.Application.Accounts.Dtos;

/// <param name="AccessToken">Sent as <c>Authorization: Bearer</c> with every request until <paramref name="ExpiresAt"/>.</param>
public sealed record SignedInDto(string AccessToken, DateTime ExpiresAt, AccountDto Account);

namespace ToJePrivela.Application.Accounts.Dtos;

public sealed record AccountDto(int Id, string Provider, string DisplayName, string? Email, bool IsAdmin);

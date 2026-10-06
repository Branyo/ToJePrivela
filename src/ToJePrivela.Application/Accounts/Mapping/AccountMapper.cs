using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Accounts.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Accounts.Mapping;

public static class AccountMapper
{
    public static AccountDto ToDto(Account account) => new(account.Id, account.Name, account.IsAdmin);

    public static SignedInDto ToSignedInDto(Account account, AccessToken token) =>
        new(token.Value, token.ExpiresAt, ToDto(account));
}

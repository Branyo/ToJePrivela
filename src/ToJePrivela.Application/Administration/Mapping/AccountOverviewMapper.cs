using ToJePrivela.Application.Administration.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Administration.Mapping;

public static class AccountOverviewMapper
{
    public static AccountOverviewDto ToDto(Account account) =>
        new(account.Id, account.Name, account.IsAdmin, account.CreatedAt, account.LastSignedInAt);

    public static IReadOnlyList<AccountOverviewDto> ToDtos(IEnumerable<Account> accounts) =>
        accounts.Select(ToDto).ToList();
}

using ToJePrivela.Application.Administration.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Administration.Mapping;

public static class AccountOverviewMapper
{
    /// <param name="adminId">The admin looking, whose own login cannot be deleted.</param>
    public static AccountOverviewDto ToDto(Account account, int adminId) =>
        new(
            account.Id,
            account.Name,
            account.IsAdmin,
            account.CreatedAt,
            account.LastSignedInAt,
            CanDelete: account.Id != adminId && account.Id != Account.ReservedId);

    public static IReadOnlyList<AccountOverviewDto> ToDtos(IEnumerable<Account> accounts, int adminId) =>
        accounts.Select(account => ToDto(account, adminId)).ToList();
}

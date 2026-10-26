using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Administration.Dtos;

namespace ToJePrivela.Application.Administration.Mapping;

public static class AccountOverviewMapper
{
    /// <param name="adminId">The admin looking, whose own login cannot be deleted.</param>
    public static AccountOverviewDto ToDto(LoginSummary login, int adminId) =>
        new(
            login.Id,
            login.Name,
            login.IsAdmin,
            login.CreatedAt,
            login.LastSignedInAt,
            CanDelete: AccountDeletion.Refusal(login.Id, adminId) is null);

    public static IReadOnlyList<AccountOverviewDto> ToDtos(IEnumerable<LoginSummary> logins, int adminId) =>
        logins.Select(login => ToDto(login, adminId)).ToList();
}

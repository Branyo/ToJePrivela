namespace ToJePrivela.Application.Administration.Dtos;

/// <summary>A login as an admin sees it in the list of all logins.</summary>
/// <param name="LastSignedInAt">Null for a login nobody has signed in to yet (an admin seeded from configuration).</param>
/// <param name="CanDelete">
/// False for the admin's own login and for the reserved one, which <c>DELETE /api/admin/accounts/{id}</c> refuses.
/// </param>
public sealed record AccountOverviewDto(
    int Id,
    string Name,
    bool IsAdmin,
    DateTime CreatedAt,
    DateTime? LastSignedInAt,
    bool CanDelete);

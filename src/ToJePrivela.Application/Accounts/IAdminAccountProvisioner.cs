namespace ToJePrivela.Application.Accounts;

/// <summary>Stores the configured admins (<see cref="AdminAccountsOptions"/>) as admin accounts; run on startup.</summary>
public interface IAdminAccountProvisioner
{
    Task ProvisionAsync(CancellationToken cancellationToken = default);
}

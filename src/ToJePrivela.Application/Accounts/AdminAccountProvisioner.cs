using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Accounts;

/// <summary>
/// For every configured admin: an account with that email at that provider is made admin; otherwise one is provisioned
/// and bound at the first sign-in with the email. The <em>first</em> configured admin who has no account yet takes over
/// the reserved account (<see cref="Account.ReservedId"/>), and with it everything stored before sign-in existed.
/// Admin rights are only ever granted here: removing an admin from configuration does not revoke them.
/// </summary>
public sealed class AdminAccountProvisioner : IAdminAccountProvisioner
{
    private readonly IAccountRepository _accounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly AdminAccountsOptions _options;
    private readonly ILogger<AdminAccountProvisioner> _logger;

    public AdminAccountProvisioner(
        IAccountRepository accounts,
        IUnitOfWork unitOfWork,
        IOptions<AdminAccountsOptions> options,
        ILogger<AdminAccountProvisioner> logger)
    {
        _accounts = accounts;
        _unitOfWork = unitOfWork;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProvisionAsync(CancellationToken cancellationToken = default)
    {
        if (_options.Admins.Count == 0)
        {
            _logger.LogWarning("No admin is configured (Authentication:Admins); nobody can manage questions.");
            return;
        }

        for (var index = 0; index < _options.Admins.Count; index++)
        {
            var admin = _options.Admins[index];
            var provider = admin.Provider!.Value;
            var email = Account.NormalizeEmail(admin.Email);

            if (await _accounts.GetByEmailAsync(provider, email, cancellationToken) is { } existing)
            {
                existing.GrantAdmin();
                continue;
            }

            if (index == 0 && await _accounts.GetReservedAsync(cancellationToken) is { } reserved)
            {
                reserved.ProvisionFor(provider, email);
                _logger.LogInformation("The data stored before sign-in now belongs to the first admin ({Provider}).", provider);
                continue;
            }

            await _accounts.AddAsync(Account.ProvisionAdmin(provider, email), cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("{Count} admin login(s) provisioned.", _options.Admins.Count);
    }
}

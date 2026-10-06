using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Accounts;

/// <summary>
/// Makes the stored admins match configuration, which is the only place admin rights come from:
/// <list type="bullet">
/// <item>A configured admin whose login exists is made admin, and its password is set to the configured one when it
/// differs. Otherwise whoever created a login with that name first would become admin with their own password.</item>
/// <item>A configured admin without a login gets one. The first of them takes over the reserved account
/// (<see cref="Account.ReservedId"/>) while it is still reserved, and with it everything stored before logins.</item>
/// <item>Every other admin loses the right.</item>
/// </list>
/// </summary>
public sealed class AdminAccountProvisioner : IAdminAccountProvisioner
{
    private readonly IAccountRepository _accounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly TimeProvider _timeProvider;
    private readonly AdminAccountsOptions _options;
    private readonly ILogger<AdminAccountProvisioner> _logger;

    public AdminAccountProvisioner(
        IAccountRepository accounts,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        TimeProvider timeProvider,
        IOptions<AdminAccountsOptions> options,
        ILogger<AdminAccountProvisioner> logger)
    {
        _accounts = accounts;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _timeProvider = timeProvider;
        _options = options.Value;
        _logger = logger;
    }

    public async Task ProvisionAsync(CancellationToken cancellationToken = default)
    {
        if (_options.Admins.Count == 0)
        {
            _logger.LogWarning("No admin is configured (Authentication:Admins); nobody can manage questions.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var reserved = await _accounts.GetReservedAsync(cancellationToken);
        var adminKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var admin in _options.Admins)
        {
            var name = Account.NormalizeName(admin.Name);

            if (!adminKeys.Add(NameKeys.Of(name)))
            {
                continue;
            }

            if (await _accounts.GetByNameAsync(name, cancellationToken) is { } existing)
            {
                existing.GrantAdmin();

                if (_passwordHasher.Verify(existing.PasswordHash!, admin.Password) != PasswordCheck.Succeeded)
                {
                    existing.ChangePasswordHash(_passwordHasher.Hash(admin.Password));
                }

                continue;
            }

            if (reserved is not null)
            {
                reserved.TakeOverReserved(name, _passwordHasher.Hash(admin.Password), now);
                reserved = null;
                _logger.LogInformation("The data stored before logins existed now belongs to admin {Name}.", name);
                continue;
            }

            await _accounts.AddAsync(Account.CreateAdmin(name, _passwordHasher.Hash(admin.Password), now), cancellationToken);
        }

        foreach (var former in await _accounts.GetAdminsAsync(cancellationToken))
        {
            if (!adminKeys.Contains(former.NameKey))
            {
                former.RevokeAdmin();
                _logger.LogInformation("{Name} is no longer configured as an admin.", former.Name);
            }
        }

        if (reserved is not null && _options.Admins.Count > 0)
        {
            _logger.LogWarning(
                "Every configured admin already has a login, so the data stored before logins existed stays with the "
                + "reserved account until an admin without a login is configured.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("{Count} admin login(s) provisioned.", adminKeys.Count);
    }
}

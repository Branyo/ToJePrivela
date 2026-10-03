using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Accounts.Dtos;
using ToJePrivela.Application.Accounts.Mapping;
using ToJePrivela.Application.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Accounts;

public sealed class AccountService : IAccountService
{
    private readonly IEnumerable<IExternalIdentityVerifier> _verifiers;
    private readonly IAccountRepository _accounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccessTokenIssuer _tokenIssuer;
    private readonly ICurrentAccount _currentAccount;
    private readonly TimeProvider _timeProvider;

    public AccountService(
        IEnumerable<IExternalIdentityVerifier> verifiers,
        IAccountRepository accounts,
        IUnitOfWork unitOfWork,
        IAccessTokenIssuer tokenIssuer,
        ICurrentAccount currentAccount,
        TimeProvider timeProvider)
    {
        _verifiers = verifiers;
        _accounts = accounts;
        _unitOfWork = unitOfWork;
        _tokenIssuer = tokenIssuer;
        _currentAccount = currentAccount;
        _timeProvider = timeProvider;
    }

    public Result<IReadOnlyList<SignInProviderDto>> GetProviders() =>
        Result.Success<IReadOnlyList<SignInProviderDto>>(_verifiers
            .Where(verifier => verifier.ClientId is not null)
            .OrderBy(verifier => verifier.Provider)
            .Select(verifier => new SignInProviderDto(verifier.Provider.ToString(), verifier.ClientId!))
            .ToList());

    public async Task<Result<SignedInDto>> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure<SignedInDto>(invalid);
        }

        var provider = request.Provider!.Value;
        var verifier = _verifiers.FirstOrDefault(v => v.Provider == provider && v.ClientId is not null);

        if (verifier is null)
        {
            return Result.Failure<SignedInDto>(AccountErrors.ProviderNotConfigured(provider));
        }

        ExternalIdentity? identity;

        try
        {
            identity = await verifier.VerifyAsync(request.Token, cancellationToken);
        }
        catch (IdentityProviderUnavailableException)
        {
            return Result.Failure<SignedInDto>(AccountErrors.ProviderUnavailable(provider));
        }

        if (identity is null || identity.Provider != provider)
        {
            return Result.Failure<SignedInDto>(AccountErrors.InvalidToken(provider));
        }

        Account account;

        try
        {
            account = await SignInAsync(identity, cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            // The same identity signed in for the first time twice at once and the other request stored it first.
            _unitOfWork.DiscardChanges();
            account = await SignInAsync(identity, cancellationToken);
        }

        return Result.Success(AccountMapper.ToSignedInDto(account, _tokenIssuer.Issue(account)));
    }

    public async Task<Result<AccountDto>> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(_currentAccount.Id, cancellationToken);

        return account is null
            ? Result.Failure<AccountDto>(AccountErrors.UnknownAccount)
            : Result.Success(AccountMapper.ToDto(account));
    }

    private async Task<Account> SignInAsync(ExternalIdentity identity, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var account = await _accounts.GetBoundAsync(identity.Provider, identity.ExternalId, cancellationToken)
            ?? (identity.Email is { } email
                ? await _accounts.GetProvisionedAsync(identity.Provider, Account.NormalizeEmail(email), cancellationToken)
                : null);

        if (account is null)
        {
            account = Account.Register(identity.Provider, identity.ExternalId, identity.Email, identity.DisplayName, now);
            await _accounts.AddAsync(account, cancellationToken);
        }
        else
        {
            account.SignIn(identity.ExternalId, identity.Email, identity.DisplayName, now);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return account;
    }
}

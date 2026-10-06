using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Accounts.Dtos;
using ToJePrivela.Application.Accounts.Mapping;
using ToJePrivela.Application.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Accounts;

public sealed class AccountService : IAccountService
{
    private readonly IAccountRepository _accounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAccessTokenIssuer _tokenIssuer;
    private readonly ICurrentAccount _currentAccount;
    private readonly TimeProvider _timeProvider;

    public AccountService(
        IAccountRepository accounts,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IAccessTokenIssuer tokenIssuer,
        ICurrentAccount currentAccount,
        TimeProvider timeProvider)
    {
        _accounts = accounts;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _tokenIssuer = tokenIssuer;
        _currentAccount = currentAccount;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SignedInDto>> SignInAsync(SignInRequest request, CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure<SignedInDto>(invalid);
        }

        var name = Account.NormalizeName(request.Name);
        var account = await _accounts.GetByNameAsync(name, cancellationToken);

        if (account is null || account.IsReserved)
        {
            return Result.Failure<SignedInDto>(AccountErrors.UnknownLogin(name));
        }

        var password = request.Password ?? string.Empty;
        var check = password.Length == 0
            ? PasswordCheck.Failed
            : _passwordHasher.Verify(account.PasswordHash!, password);

        if (check == PasswordCheck.Failed)
        {
            return Result.Failure<SignedInDto>(AccountErrors.WrongPassword);
        }

        if (check == PasswordCheck.SucceededRehashNeeded)
        {
            account.ChangePasswordHash(_passwordHasher.Hash(password));
        }

        account.RecordSignIn(_timeProvider.GetUtcNow().UtcDateTime);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(AccountMapper.ToSignedInDto(account, _tokenIssuer.Issue(account)));
    }

    public async Task<Result<SignedInDto>> CreateAsync(CreateAccountRequest request, CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure<SignedInDto>(invalid);
        }

        var name = Account.NormalizeName(request.Name);

        if (await _accounts.GetByNameAsync(name, cancellationToken) is not null)
        {
            return Result.Failure<SignedInDto>(AccountErrors.NameTaken(name));
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var account = Account.Register(name, _passwordHasher.Hash(request.Password), now);
        account.RecordSignIn(now);

        await _accounts.AddAsync(account, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            // Another request took the same name between the check above and this save.
            return Result.Failure<SignedInDto>(AccountErrors.NameTaken(name));
        }

        return Result.Success(AccountMapper.ToSignedInDto(account, _tokenIssuer.Issue(account)));
    }

    public Task<Result<AccountDto>> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        GetByIdAsync(_currentAccount.Id, cancellationToken);

    public async Task<Result<AccountDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var account = await _accounts.GetByIdAsync(id, cancellationToken);

        return account is null || account.IsReserved
            ? Result.Failure<AccountDto>(AccountErrors.UnknownAccount)
            : Result.Success(AccountMapper.ToDto(account));
    }
}

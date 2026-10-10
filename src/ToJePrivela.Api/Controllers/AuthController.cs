using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Accounts;
using ToJePrivela.Application.Accounts.Dtos;

namespace ToJePrivela.Api.Controllers;

/// <summary>
/// Signing in with a login name and password, and creating a login. Both answer with an access token to send as
/// <c>Authorization: Bearer</c> with every following request.
/// </summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IAccountService _accounts;

    public AuthController(IAccountService accounts)
    {
        _accounts = accounts;
    }

    /// <summary>404 <c>Auth.UnknownLogin</c> when no login has the name, so the client can offer to create it.</summary>
    [HttpPost("sign-in")]
    [AllowAnonymous]
    [EnableRateLimiting(SignInRateLimitOptions.PolicyName)]
    [SuccessIsFree]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<SignedInDto>> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken) =>
        (await _accounts.SignInAsync(request, cancellationToken)).ToActionResult();

    /// <summary>Creates a login (never an admin) and signs it in.</summary>
    [HttpPost("accounts")]
    [AllowAnonymous]
    [EnableRateLimiting(SignInRateLimitOptions.PolicyName)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<SignedInDto>> CreateAccount(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken) =>
        (await _accounts.CreateAsync(request, cancellationToken))
            .ToCreatedResult(nameof(GetCurrentAccount), _ => new { });

    /// <summary>
    /// Changes the signed-in login's password; 400 <c>Auth.CurrentPasswordWrong</c> when the current one is wrong.
    /// Ends every other sign-in of the login and answers with a fresh token for this one. 409
    /// <c>Auth.AdminPasswordFromConfig</c> for an admin, whose password the server configuration sets. Rate limited per
    /// login whatever the address, so a token left on a shared device cannot be used to guess the password at speed.
    /// </summary>
    [HttpPut("password")]
    [EnableRateLimiting(SignInRateLimitOptions.PolicyName)]
    [SuccessIsFree]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<SignedInDto>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken) =>
        (await _accounts.ChangePasswordAsync(request, cancellationToken)).ToActionResult();

    /// <summary>The signed-in login, admin flag included.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccountDto>> GetCurrentAccount(CancellationToken cancellationToken) =>
        (await _accounts.GetCurrentAsync(cancellationToken)).ToActionResult();
}

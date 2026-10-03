using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Accounts;
using ToJePrivela.Application.Accounts.Dtos;

namespace ToJePrivela.Api.Controllers;

/// <summary>
/// The browser signs in with Google or Facebook itself and posts the token it got; the answer carries this API's own
/// access token for every following request.
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

    /// <summary>The configured providers, with the public id each one's browser SDK needs.</summary>
    [HttpGet("providers")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<SignInProviderDto>> GetProviders() => _accounts.GetProviders().ToActionResult();

    [HttpPost("sign-in")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<SignedInDto>> SignIn([FromBody] SignInRequest request, CancellationToken cancellationToken) =>
        (await _accounts.SignInAsync(request, cancellationToken)).ToActionResult();

    /// <summary>The signed-in account, admin flag included.</summary>
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AccountDto>> GetCurrentAccount(CancellationToken cancellationToken) =>
        (await _accounts.GetCurrentAsync(cancellationToken)).ToActionResult();
}

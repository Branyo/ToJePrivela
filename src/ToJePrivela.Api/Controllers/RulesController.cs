using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Rules;
using ToJePrivela.Application.Rules.Dtos;

namespace ToJePrivela.Api.Controllers;

/// <summary>The limits the game enforces, for clients to validate and render with; readable before signing in.</summary>
[ApiController]
[AllowAnonymous]
[Route("api/rules")]
[Produces("application/json")]
public sealed class RulesController : ControllerBase
{
    private readonly IGameRulesService _rules;

    public RulesController(IGameRulesService rules)
    {
        _rules = rules;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<GameRulesDto> GetRules() => _rules.Get().ToActionResult();
}

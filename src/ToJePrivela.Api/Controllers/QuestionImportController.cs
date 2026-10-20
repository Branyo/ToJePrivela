using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.QuestionImport;
using ToJePrivela.Application.QuestionImport.Dtos;

namespace ToJePrivela.Api.Controllers;

/// <summary>Adds questions written in the AI generator's JSON format to a category; admins only.</summary>
[ApiController]
[Route("api/question-categories/{id:int}/imported-questions")]
[Produces("application/json")]
[Authorize(Policy = AuthorizationPolicies.Admin)]
public sealed class QuestionImportController : ControllerBase
{
    private readonly IQuestionImportService _import;

    public QuestionImportController(IQuestionImportService import)
    {
        _import = import;
    }

    /// <summary>Stores the usable questions and reports the others (unusable or already in the category) by position.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportedQuestionsDto>> ImportQuestions(
        int id,
        [FromBody] ImportQuestionsRequest request,
        CancellationToken cancellationToken) =>
        (await _import.ImportAsync(id, request, cancellationToken)).ToActionResult();
}

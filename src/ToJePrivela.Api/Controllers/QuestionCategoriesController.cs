using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.QuestionCategories;
using ToJePrivela.Application.QuestionCategories.Dtos;

namespace ToJePrivela.Api.Controllers;

/// <summary>Categories are immutable: there is no PUT, delete and create a new one instead.</summary>
[ApiController]
[Route("api/question-categories")]
[Produces("application/json")]
public sealed class QuestionCategoriesController : ControllerBase
{
    private readonly IQuestionCategoryService _categories;

    public QuestionCategoriesController(IQuestionCategoryService categories)
    {
        _categories = categories;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<QuestionCategoryDto>>> GetQuestionCategories(CancellationToken cancellationToken) =>
        (await _categories.GetAllAsync(cancellationToken)).ToActionResult();

    [HttpGet("{id:int}", Name = nameof(GetQuestionCategory))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionCategoryDto>> GetQuestionCategory(int id, CancellationToken cancellationToken) =>
        (await _categories.GetByIdAsync(id, cancellationToken)).ToActionResult();

    /// <summary>Creates the category and generates its AI questions; nothing is stored when generation fails.</summary>
    [HttpPost]
    [EnableRateLimiting(AiRateLimitOptions.PolicyName)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<CreatedQuestionCategoryDto>> CreateQuestionCategory(
        [FromBody] CreateQuestionCategoryRequest request,
        CancellationToken cancellationToken) =>
        (await _categories.CreateAsync(request, cancellationToken))
            .ToCreatedResult(nameof(GetQuestionCategory), category => new { id = category.Id });

    /// <summary>Deletes the category together with all of its questions.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteQuestionCategory(int id, CancellationToken cancellationToken) =>
        (await _categories.DeleteAsync(id, cancellationToken)).ToActionResult();

    /// <summary>Generates AI questions and adds them to the category, skipping ones it already has.</summary>
    [HttpPost("{id:int}/ai-questions")]
    [EnableRateLimiting(AiRateLimitOptions.PolicyName)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GeneratedAiQuestionsDto>> GenerateAiQuestions(
        int id,
        [FromBody] GenerateAiQuestionsRequest request,
        CancellationToken cancellationToken) =>
        (await _categories.GenerateAiQuestionsAsync(id, request, cancellationToken)).ToActionResult();

    /// <summary>Deletes the category's AI questions; manually added or edited ones stay.</summary>
    [HttpDelete("{id:int}/ai-questions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeletedAiQuestionsDto>> DeleteAiQuestions(int id, CancellationToken cancellationToken) =>
        (await _categories.DeleteAiQuestionsAsync(id, cancellationToken)).ToActionResult();
}

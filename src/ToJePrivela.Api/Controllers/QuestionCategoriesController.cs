using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.QuestionCategories;
using ToJePrivela.Application.QuestionCategories.Dtos;

namespace ToJePrivela.Api.Controllers;

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

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuestionCategoryDto>> CreateQuestionCategory(
        [FromBody] CreateQuestionCategoryRequest request,
        CancellationToken cancellationToken) =>
        (await _categories.CreateAsync(request, cancellationToken))
            .ToCreatedResult(nameof(GetQuestionCategory), category => new { id = category.Id });

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> UpdateQuestionCategory(
        int id,
        [FromBody] UpdateQuestionCategoryRequest request,
        CancellationToken cancellationToken) =>
        (await _categories.UpdateAsync(id, request, cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteQuestionCategory(int id, CancellationToken cancellationToken) =>
        (await _categories.DeleteAsync(id, cancellationToken)).ToActionResult();
}

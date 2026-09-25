using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Questions;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Api.Controllers;

[ApiController]
[Route("api/questions")]
[Produces("application/json")]
public sealed class QuestionsController : ControllerBase
{
    private readonly IQuestionService _questions;

    public QuestionsController(IQuestionService questions)
    {
        _questions = questions;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<QuestionDto>>> GetQuestions(
        [FromQuery] QuestionFilter filter,
        CancellationToken cancellationToken) =>
        (await _questions.GetAsync(filter, cancellationToken)).ToActionResult();

    /// <summary>
    /// A random question among the least viewed ones in the given categories (every category when none
    /// are given). Does not count as a view — POST to <c>{id}/views</c> once the question is shown.
    /// </summary>
    [HttpGet("random")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionDto>> GetRandomQuestion(
        [FromQuery] RandomQuestionFilter filter,
        CancellationToken cancellationToken) =>
        (await _questions.GetRandomAsync(filter, cancellationToken)).ToActionResult();

    /// <summary>Records that the question was shown; returns it with the updated view count.</summary>
    [HttpPost("{id:int}/views")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuestionDto>> RecordQuestionView(int id, CancellationToken cancellationToken) =>
        (await _questions.RecordViewAsync(id, cancellationToken)).ToActionResult();

    [HttpGet("{id:int}", Name = nameof(GetQuestion))]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionDto>> GetQuestion(int id, CancellationToken cancellationToken) =>
        (await _questions.GetByIdAsync(id, cancellationToken)).ToActionResult();

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<QuestionDto>> CreateQuestion(
        [FromBody] CreateQuestionRequest request,
        CancellationToken cancellationToken) =>
        (await _questions.CreateAsync(request, cancellationToken))
            .ToCreatedResult(nameof(GetQuestion), question => new { id = question.Id });

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateQuestion(
        int id,
        [FromBody] UpdateQuestionRequest request,
        CancellationToken cancellationToken) =>
        (await _questions.UpdateAsync(id, request, cancellationToken)).ToActionResult();

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteQuestion(int id, CancellationToken cancellationToken) =>
        (await _questions.DeleteAsync(id, cancellationToken)).ToActionResult();
}

using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Questions.Dtos;
using ToJePrivela.Application.Questions.Mapping;

namespace ToJePrivela.Application.Questions;

public sealed class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questions;
    private readonly IQuestionCategoryRepository _categories;
    private readonly IBadPointsPicker _badPoints;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public QuestionService(
        IQuestionRepository questions,
        IQuestionCategoryRepository categories,
        IBadPointsPicker badPoints,
        TimeProvider timeProvider,
        IUnitOfWork unitOfWork)
    {
        _questions = questions;
        _categories = categories;
        _badPoints = badPoints;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<QuestionDto>>> GetAsync(QuestionFilter filter, CancellationToken cancellationToken = default)
    {
        var questions = await _questions.FindAsync(filter.CategoryId, filter.Source, cancellationToken);
        return Result.Success(QuestionMapper.ToDtos(questions));
    }

    public async Task<Result<QuestionDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var question = await _questions.GetByIdAsync(id, cancellationToken);

        return question is null
            ? Result.Failure<QuestionDto>(QuestionErrors.NotFound(id))
            : Result.Success(QuestionMapper.ToDto(question));
    }

    public async Task<Result<QuestionDto>> CreateAsync(CreateQuestionRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetByIdAsync(request.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<QuestionDto>(QuestionErrors.UnknownCategory(request.CategoryId));
        }

        var question = QuestionMapper.ToEntity(
            request,
            category,
            request.BadPoints ?? _badPoints.Pick(),
            _timeProvider.GetUtcNow().UtcDateTime);

        await _questions.AddAsync(question, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(QuestionMapper.ToDto(question));
    }

    public async Task<Result> UpdateAsync(int id, UpdateQuestionRequest request, CancellationToken cancellationToken = default)
    {
        var question = await _questions.GetByIdAsync(id, cancellationToken);

        if (question is null)
        {
            return Result.Failure(QuestionErrors.NotFound(id));
        }

        var category = await _categories.GetByIdAsync(request.CategoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure(QuestionErrors.UnknownCategory(request.CategoryId));
        }

        question.Update(request.Text, request.Answer, category, request.BadPoints ?? question.BadPoints);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var question = await _questions.GetByIdAsync(id, cancellationToken);

        if (question is null)
        {
            return Result.Failure(QuestionErrors.NotFound(id));
        }

        _questions.Remove(question);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

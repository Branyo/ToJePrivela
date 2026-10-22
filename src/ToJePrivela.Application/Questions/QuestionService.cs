using ToJePrivela.Application.Abstractions.Identity;
using ToJePrivela.Application.Abstractions.Localization;
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
    private readonly ICurrentLanguage _language;
    private readonly ICurrentAccount _currentAccount;
    private readonly IUnitOfWork _unitOfWork;

    public QuestionService(
        IQuestionRepository questions,
        IQuestionCategoryRepository categories,
        IBadPointsPicker badPoints,
        TimeProvider timeProvider,
        ICurrentLanguage language,
        ICurrentAccount currentAccount,
        IUnitOfWork unitOfWork)
    {
        _questions = questions;
        _categories = categories;
        _badPoints = badPoints;
        _timeProvider = timeProvider;
        _language = language;
        _currentAccount = currentAccount;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<QuestionDto>>> GetAsync(QuestionFilter filter, CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(filter) is { } invalid)
        {
            return Result.Failure<IReadOnlyList<QuestionDto>>(invalid);
        }

        var questions = await _questions.FindAsync(filter.CategoryId!.Value, filter.Source, cancellationToken);
        return Result.Success(QuestionMapper.ToDtos(questions, _language.Language));
    }

    public async Task<Result<QuestionDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var question = await _questions.GetByIdAsync(id, cancellationToken);

        return question is null
            ? Result.Failure<QuestionDto>(QuestionErrors.NotFound(id))
            : Result.Success(QuestionMapper.ToDto(question, _language.Language));
    }

    public async Task<Result<QuestionDto>> GetRandomAsync(RandomQuestionFilter filter, CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(filter) is { } invalid)
        {
            return Result.Failure<QuestionDto>(invalid);
        }

        var invalidIds = filter.CategoryIds.Where(id => id <= 0).Distinct().ToList();

        if (invalidIds.Count > 0)
        {
            return Result.Failure<QuestionDto>(QuestionErrors.InvalidCategoryIds(invalidIds));
        }

        var categoryIds = filter.CategoryIds.Distinct().ToList();

        if (categoryIds.Count > 0)
        {
            var missingIds = await _categories.GetMissingIdsAsync(categoryIds, cancellationToken);

            if (missingIds.Count > 0)
            {
                return Result.Failure<QuestionDto>(QuestionErrors.UnknownCategories(missingIds));
            }
        }

        var question = await _questions.GetRandomLeastViewedAsync(_currentAccount.Id, categoryIds, cancellationToken);

        return question is null
            ? Result.Failure<QuestionDto>(QuestionErrors.NoneAvailable())
            : Result.Success(QuestionMapper.ToDto(question, _language.Language));
    }

    public async Task<Result> RecordViewAsync(int id, CancellationToken cancellationToken = default) =>
        await _questions.RecordViewAsync(_currentAccount.Id, id, _timeProvider.GetUtcNow().UtcDateTime, cancellationToken)
            ? Result.Success()
            : Result.Failure(QuestionErrors.NotFound(id));

    public async Task<Result<QuestionDto>> CreateAsync(CreateQuestionRequest request, CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure<QuestionDto>(invalid);
        }

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

        return Result.Success(QuestionMapper.ToDto(question, _language.Language));
    }

    public async Task<Result> UpdateAsync(int id, UpdateQuestionRequest request, CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure(invalid);
        }

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

        question.Update(request.TextSk, request.TextEn ?? question.TextEn, request.Answer, category, request.BadPoints ?? question.BadPoints);
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

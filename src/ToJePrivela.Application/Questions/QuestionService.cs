using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.Questions.Dtos;
using ToJePrivela.Application.Questions.Mapping;

namespace ToJePrivela.Application.Questions;

public sealed class QuestionService : IQuestionService
{
    private readonly IQuestionRepository _questions;
    private readonly IQuestionCategoryRepository _categories;
    private readonly IQuestionGenerator _questionGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public QuestionService(
        IQuestionRepository questions,
        IQuestionCategoryRepository categories,
        IQuestionGenerator questionGenerator,
        IUnitOfWork unitOfWork)
    {
        _questions = questions;
        _categories = categories;
        _questionGenerator = questionGenerator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<QuestionDto>>> GetAsync(QuestionFilter filter, CancellationToken cancellationToken = default)
    {
        var questions = await _questions.FindAsync(filter.Category, filter.Difficulty, cancellationToken);
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
        var question = QuestionMapper.ToEntity(request);

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

        question.Update(request.Text, request.Answer, request.Category, request.Difficulty);
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

    public async Task<Result<IReadOnlyList<GeneratedQuestionDto>>> GenerateAsync(
        GenerateQuestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = request.Category?.Trim();

        if (!string.IsNullOrEmpty(category))
        {
            if (await _categories.GetByNameAsync(category, cancellationToken) is null)
            {
                return Result.Failure<IReadOnlyList<GeneratedQuestionDto>>(QuestionErrors.UnknownCategory(category));
            }
        }
        else
        {
            // No category asked for: pick one that is actually in use.
            category = (await _categories.GetRandomAsync(cancellationToken))?.Name;
        }

        var generated = await _questionGenerator.GenerateAsync(
            new QuestionGenerationRequest(category, request.Count, request.Language),
            cancellationToken);

        return generated.Count == 0
            ? Result.Failure<IReadOnlyList<GeneratedQuestionDto>>(QuestionErrors.GenerationFailed)
            : Result.Success(QuestionMapper.ToDtos(generated));
    }
}

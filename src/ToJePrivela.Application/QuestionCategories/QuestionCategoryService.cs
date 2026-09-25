using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionCategories.Mapping;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.QuestionGeneration.Mapping;
using ToJePrivela.Application.Questions.Mapping;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionCategories;

public sealed class QuestionCategoryService : IQuestionCategoryService
{
    private readonly IQuestionCategoryRepository _categories;
    private readonly IQuestionRepository _questions;
    private readonly IPlayerRepository _players;
    private readonly IQuestionGenerationService _questionGeneration;
    private readonly IUnitOfWork _unitOfWork;

    public QuestionCategoryService(
        IQuestionCategoryRepository categories,
        IQuestionRepository questions,
        IPlayerRepository players,
        IQuestionGenerationService questionGeneration,
        IUnitOfWork unitOfWork)
    {
        _categories = categories;
        _questions = questions;
        _players = players;
        _questionGeneration = questionGeneration;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<QuestionCategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categories.GetAllAsync(cancellationToken);
        return Result.Success(QuestionCategoryMapper.ToDtos(categories));
    }

    public async Task<Result<QuestionCategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetByIdAsync(id, cancellationToken);

        return category is null
            ? Result.Failure<QuestionCategoryDto>(QuestionCategoryErrors.NotFound(id))
            : Result.Success(QuestionCategoryMapper.ToDto(category));
    }

    public async Task<Result<CreatedQuestionCategoryDto>> CreateAsync(CreateQuestionCategoryRequest request, CancellationToken cancellationToken = default)
    {
        // Both checks run before generation, so a rejected request never pays for AI calls.
        if (await _categories.GetByNameAsync(request.Name, cancellationToken) is not null)
        {
            return Result.Failure<CreatedQuestionCategoryDto>(QuestionCategoryErrors.NameTaken(request.Name));
        }

        if (request.AddedByPlayerId is int playerId && !await _players.ExistsAsync(playerId, cancellationToken))
        {
            return Result.Failure<CreatedQuestionCategoryDto>(QuestionCategoryErrors.UnknownPlayer(playerId));
        }

        var category = QuestionCategoryMapper.ToEntity(request);
        var generation = await _questionGeneration.GenerateAsync(category, request.QuestionCount ?? 0, cancellationToken);

        if (generation.Failed)
        {
            return Result.Failure<CreatedQuestionCategoryDto>(QuestionGenerationErrors.GenerationFailed);
        }

        await _categories.AddAsync(category, cancellationToken);
        await _questions.AddRangeAsync(generation.Questions, cancellationToken);

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException)
        {
            // Another request created the same name while this one was generating.
            return Result.Failure<CreatedQuestionCategoryDto>(QuestionCategoryErrors.NameTaken(request.Name));
        }

        return Result.Success(QuestionCategoryMapper.ToCreatedDto(category, generation));
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return Result.Failure(QuestionCategoryErrors.NotFound(id));
        }

        // The database cascades the delete to the category's questions.
        _categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<GeneratedAiQuestionsDto>> GenerateAiQuestionsAsync(
        int id,
        GenerateAiQuestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<GeneratedAiQuestionsDto>(QuestionCategoryErrors.NotFound(id));
        }

        var generation = await _questionGeneration.GenerateAsync(category, request.Count ?? 0, cancellationToken);

        if (generation.Failed)
        {
            return Result.Failure<GeneratedAiQuestionsDto>(QuestionGenerationErrors.GenerationFailed);
        }

        await _questions.AddRangeAsync(generation.Questions, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new GeneratedAiQuestionsDto(
            QuestionGenerationMapper.ToSummaryDto(generation),
            QuestionMapper.ToDtos(generation.Questions)));
    }

    public async Task<Result<DeletedAiQuestionsDto>> DeleteAiQuestionsAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await _categories.ExistsAsync(id, cancellationToken))
        {
            return Result.Failure<DeletedAiQuestionsDto>(QuestionCategoryErrors.NotFound(id));
        }

        var aiQuestions = await _questions.FindAsync(id, QuestionSource.Ai, cancellationToken);

        _questions.RemoveRange(aiQuestions);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new DeletedAiQuestionsDto(aiQuestions.Count));
    }
}

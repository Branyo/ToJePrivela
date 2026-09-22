using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionCategories.Mapping;

namespace ToJePrivela.Application.QuestionCategories;

public sealed class QuestionCategoryService : IQuestionCategoryService
{
    private readonly IQuestionCategoryRepository _categories;
    private readonly IPlayerRepository _players;
    private readonly IUnitOfWork _unitOfWork;

    public QuestionCategoryService(
        IQuestionCategoryRepository categories,
        IPlayerRepository players,
        IUnitOfWork unitOfWork)
    {
        _categories = categories;
        _players = players;
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

    public async Task<Result<QuestionCategoryDto>> CreateAsync(CreateQuestionCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (await _categories.GetByNameAsync(request.Name, cancellationToken) is not null)
        {
            return Result.Failure<QuestionCategoryDto>(QuestionCategoryErrors.NameTaken(request.Name));
        }

        if (request.AddedByPlayerId is int playerId && !await _players.ExistsAsync(playerId, cancellationToken))
        {
            return Result.Failure<QuestionCategoryDto>(QuestionCategoryErrors.UnknownPlayer(playerId));
        }

        var category = QuestionCategoryMapper.ToEntity(request);

        await _categories.AddAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(QuestionCategoryMapper.ToDto(category));
    }

    public async Task<Result> UpdateAsync(int id, UpdateQuestionCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return Result.Failure(QuestionCategoryErrors.NotFound(id));
        }

        var duplicate = await _categories.GetByNameAsync(request.Name, cancellationToken);

        if (duplicate is not null && duplicate.Id != id)
        {
            return Result.Failure(QuestionCategoryErrors.NameTaken(request.Name));
        }

        category.Rename(request.Name);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return Result.Failure(QuestionCategoryErrors.NotFound(id));
        }

        _categories.Remove(category);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

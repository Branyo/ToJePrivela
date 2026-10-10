using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Abstractions.Localization;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionCategories.Mapping;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.QuestionGeneration.Mapping;
using ToJePrivela.Application.Questions.Mapping;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.QuestionCategories;

public sealed class QuestionCategoryService : IQuestionCategoryService
{
    private readonly IQuestionCategoryRepository _categories;
    private readonly IQuestionRepository _questions;
    private readonly IQuestionGenerationService _questionGeneration;
    private readonly ITextTranslator _translator;
    private readonly ICurrentLanguage _language;
    private readonly IUnitOfWork _unitOfWork;

    public QuestionCategoryService(
        IQuestionCategoryRepository categories,
        IQuestionRepository questions,
        IQuestionGenerationService questionGeneration,
        ITextTranslator translator,
        ICurrentLanguage language,
        IUnitOfWork unitOfWork)
    {
        _categories = categories;
        _questions = questions;
        _questionGeneration = questionGeneration;
        _translator = translator;
        _language = language;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<QuestionCategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var categories = await _categories.GetAllCountedAsync(cancellationToken);
        return Result.Success(QuestionCategoryMapper.ToDtos(categories, _language.Language));
    }

    public async Task<Result<QuestionCategoryDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _categories.GetCountedAsync(id, cancellationToken);

        return category is null
            ? Result.Failure<QuestionCategoryDto>(QuestionCategoryErrors.NotFound(id))
            : Result.Success(QuestionCategoryMapper.ToDto(category, _language.Language));
    }

    public async Task<Result<CreatedQuestionCategoryDto>> CreateAsync(CreateQuestionCategoryRequest request, CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure<CreatedQuestionCategoryDto>(invalid);
        }

        // Names are checked before translation and generation, so a rejected request never pays for AI calls.
        var nameSk = NormalizeOptionalName(request.NameSk);
        var nameEn = NormalizeOptionalName(request.NameEn);

        if (await FindTakenNameAsync(nameSk, nameEn, cancellationToken) is { } takenName)
        {
            return Result.Failure<CreatedQuestionCategoryDto>(QuestionCategoryErrors.NameTaken(takenName));
        }

        if (nameSk is null || nameEn is null)
        {
            var translation = nameSk is null
                ? await TranslateAsync(nameEn!, Language.En, Language.Sk, cancellationToken)
                : await TranslateAsync(nameSk, Language.Sk, Language.En, cancellationToken);

            if (translation.IsFailure)
            {
                return Result.Failure<CreatedQuestionCategoryDto>(translation.Error);
            }

            var typedName = nameSk ?? nameEn!;
            nameSk ??= translation.Value;
            nameEn ??= translation.Value;

            if (await FindTakenNameAsync(nameSk, nameEn, cancellationToken) is { } takenTranslation)
            {
                return Result.Failure<CreatedQuestionCategoryDto>(
                    QuestionCategoryErrors.TranslationNameTaken(typedName, takenTranslation));
            }
        }

        var category = new QuestionCategory(nameSk, nameEn);
        var generation = await _questionGeneration.GenerateAsync(category, request.QuestionCount ?? 0, cancellationToken);

        if (generation.Failed)
        {
            return Result.Failure<CreatedQuestionCategoryDto>(QuestionGenerationErrors.For(generation));
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
            return Result.Failure<CreatedQuestionCategoryDto>(QuestionCategoryErrors.NameTaken(category.NameIn(_language.Language)));
        }

        return Result.Success(QuestionCategoryMapper.ToCreatedDto(category, generation, _language.Language));
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
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure<GeneratedAiQuestionsDto>(invalid);
        }

        var category = await _categories.GetByIdAsync(id, cancellationToken);

        if (category is null)
        {
            return Result.Failure<GeneratedAiQuestionsDto>(QuestionCategoryErrors.NotFound(id));
        }

        var generation = await _questionGeneration.GenerateAsync(category, request.Count ?? 0, cancellationToken);

        if (generation.Failed)
        {
            return Result.Failure<GeneratedAiQuestionsDto>(QuestionGenerationErrors.For(generation));
        }

        await _questions.AddRangeAsync(generation.Questions, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new GeneratedAiQuestionsDto(
            QuestionGenerationMapper.ToSummaryDto(generation),
            QuestionMapper.ToDtos(generation.Questions, _language.Language)));
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

    private static string? NormalizeOptionalName(string? name) =>
        string.IsNullOrWhiteSpace(name) ? null : QuestionCategory.NormalizeName(name);

    /// <summary>The first given name another category already has in the same language; null when both are free.</summary>
    private async Task<string?> FindTakenNameAsync(string? nameSk, string? nameEn, CancellationToken cancellationToken)
    {
        if (nameSk is not null && await _categories.GetByNameAsync(nameSk, Language.Sk, cancellationToken) is not null)
        {
            return nameSk;
        }

        if (nameEn is not null && await _categories.GetByNameAsync(nameEn, Language.En, cancellationToken) is not null)
        {
            return nameEn;
        }

        return null;
    }

    /// <summary>One AI call; a translation the category would reject counts as no translation at all.</summary>
    private async Task<Result<string>> TranslateAsync(string name, Language from, Language to, CancellationToken cancellationToken)
    {
        string? translation;

        try
        {
            translation = await _translator.TranslateAsync(name, from, to, cancellationToken);
        }
        catch (QuestionGeneratorUnavailableException)
        {
            return Result.Failure<string>(QuestionCategoryErrors.TranslationUnavailable);
        }

        return QuestionCategory.IsValidName(translation)
            ? Result.Success(QuestionCategory.NormalizeName(translation!))
            : Result.Failure<string>(QuestionCategoryErrors.TranslationFailed);
    }
}

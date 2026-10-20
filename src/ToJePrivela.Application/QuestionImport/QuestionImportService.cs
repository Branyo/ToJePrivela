using ToJePrivela.Application.Abstractions.Localization;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionCategories;
using ToJePrivela.Application.QuestionImport.Dtos;
using ToJePrivela.Application.QuestionImport.Mapping;
using ToJePrivela.Application.Questions.Mapping;

namespace ToJePrivela.Application.QuestionImport;

/// <summary>Imports questions written in the AI generator's format into an existing category (see <see cref="IQuestionImporter"/>).</summary>
public sealed class QuestionImportService : IQuestionImportService
{
    private readonly IQuestionCategoryRepository _categories;
    private readonly IQuestionRepository _questions;
    private readonly IQuestionImporter _importer;
    private readonly ICurrentLanguage _language;
    private readonly IUnitOfWork _unitOfWork;

    public QuestionImportService(
        IQuestionCategoryRepository categories,
        IQuestionRepository questions,
        IQuestionImporter importer,
        ICurrentLanguage language,
        IUnitOfWork unitOfWork)
    {
        _categories = categories;
        _questions = questions;
        _importer = importer;
        _language = language;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ImportedQuestionsDto>> ImportAsync(
        int categoryId,
        ImportQuestionsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (RequestValidator.Validate(request) is { } invalid)
        {
            return Result.Failure<ImportedQuestionsDto>(invalid);
        }

        var items = _importer.Read(request.Json);

        if (items.IsFailure)
        {
            return Result.Failure<ImportedQuestionsDto>(items.Error);
        }

        var category = await _categories.GetByIdAsync(categoryId, cancellationToken);

        if (category is null)
        {
            return Result.Failure<ImportedQuestionsDto>(QuestionCategoryErrors.NotFound(categoryId));
        }

        var outcome = await _importer.ToQuestionsAsync(category, items.Value, cancellationToken);

        if (outcome.Questions.Count > 0)
        {
            await _questions.AddRangeAsync(outcome.Questions, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(QuestionImportMapper.ToImportedDto(outcome, _language.Language));
    }
}

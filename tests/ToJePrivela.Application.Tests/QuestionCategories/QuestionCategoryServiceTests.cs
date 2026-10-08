using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionCategories;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.QuestionCategories;

public class QuestionCategoryServiceTests
{
    private const string ValidText = "How many players are on a football pitch?";

    private readonly IQuestionCategoryRepository _categories = Substitute.For<IQuestionCategoryRepository>();
    private readonly IQuestionRepository _questions = Substitute.For<IQuestionRepository>();
    private readonly IQuestionGenerationService _generation = Substitute.For<IQuestionGenerationService>();
    private readonly ITextTranslator _translator = Substitute.For<ITextTranslator>();
    private readonly FixedCurrentLanguage _language = new();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly QuestionCategoryService _sut;

    public QuestionCategoryServiceTests()
    {
        _categories.GetByNameAsync(Arg.Any<string>(), Arg.Any<Language>(), Arg.Any<CancellationToken>())
            .Returns((QuestionCategory?)null);
        _generation.GenerateAsync(Arg.Any<QuestionCategory>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => Generated(call.Arg<QuestionCategory>(), call.Arg<int>(), created: call.Arg<int>()));

        _sut = new QuestionCategoryService(_categories, _questions, _generation, _translator, _language, _unitOfWork);
    }

    [Fact]
    public async Task GetAllAsync_MapsEveryCategory()
    {
        _categories.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([TestEntities.Category(1, "Šport", "Sport"), TestEntities.Category(2, "História", "History")]);

        var result = await _sut.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(["Šport", "História"], result.Value.Select(c => c.Name));
    }

    [Fact]
    public async Task GetAllAsync_NamesTheCategoriesInTheRequestedLanguage()
    {
        _language.Language = Language.En;
        _categories.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([TestEntities.Category(1, "Šport", "Sport"), TestEntities.Category(2, "História", "History")]);

        var result = await _sut.GetAllAsync();

        Assert.Equal(["Sport", "History"], result.Value.Select(c => c.Name));
        Assert.Equal(["Šport", "História"], result.Value.Select(c => c.NameSk));
    }

    [Fact]
    public async Task GenerateAiQuestionsAsync_RejectsTooManyQuestionsBeforePayingForAnyCall()
    {
        var result = await _sut.GenerateAiQuestionsAsync(1, new GenerateAiQuestionsRequest { Count = QuestionGenerationOptions.MaxCount + 1 });

        Assert.Equal("Request.Invalid", result.Error.Code);
        await _generation.DidNotReceive().GenerateAsync(Arg.Any<QuestionCategory>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RequiresTheQuestionCount()
    {
        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = "Šport", NameEn = "Sport" });

        Assert.Equal("Request.Invalid", result.Error.Code);
        await _categories.DidNotReceive().AddAsync(Arg.Any<QuestionCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFoundForUnknownCategory()
    {
        _categories.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);

        var result = await _sut.GetByIdAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_StoresTheCategoryWithItsGeneratedQuestionsInOneSave()
    {
        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = "Šport", NameEn = "Sport", QuestionCount = 3 });

        Assert.True(result.IsSuccess);
        Assert.Equal("Šport", result.Value.Name);
        Assert.Equal(new(3, 3, 0), result.Value.QuestionGeneration);
        await _categories.Received(1).AddAsync(
            Arg.Is<QuestionCategory>(c => c.NameSk == "Šport" && c.NameEn == "Sport"),
            Arg.Any<CancellationToken>());
        await _translator.DidNotReceiveWithAnyArgs().TranslateAsync(default!, default, default, default);
        await _questions.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Question>>(questions => questions.Count() == 3),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithZeroQuestionsStoresJustTheCategory()
    {
        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = "Šport", NameEn = "Sport", QuestionCount = 0 });

        Assert.True(result.IsSuccess);
        Assert.Equal(new(0, 0, 0), result.Value.QuestionGeneration);
        await _categories.Received(1).AddAsync(Arg.Any<QuestionCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReportsAPartialGeneration()
    {
        _generation.GenerateAsync(Arg.Any<QuestionCategory>(), 10, Arg.Any<CancellationToken>())
            .Returns(call => Generated(call.Arg<QuestionCategory>(), 10, created: 6, discarded: 2));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = "Šport", NameEn = "Sport", QuestionCount = 10 });

        Assert.True(result.IsSuccess);
        Assert.Equal(new(10, 6, 2), result.Value.QuestionGeneration);
    }

    [Fact]
    public async Task CreateAsync_StoresNothingWhenGenerationFails()
    {
        _generation.GenerateAsync(Arg.Any<QuestionCategory>(), 5, Arg.Any<CancellationToken>())
            .Returns(call => Generated(call.Arg<QuestionCategory>(), 5, created: 0));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = "Šport", NameEn = "Sport", QuestionCount = 5 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
        await _categories.DidNotReceive().AddAsync(Arg.Any<QuestionCategory>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateNameBeforeGenerating()
    {
        _categories.GetByNameAsync("Sport", Language.En, Arg.Any<CancellationToken>()).Returns(TestEntities.Category(1, "Šport", "Sport"));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = "Šport", NameEn = "Sport", QuestionCount = 5 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _generation.DidNotReceive().GenerateAsync(Arg.Any<QuestionCategory>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _categories.DidNotReceive().AddAsync(Arg.Any<QuestionCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RequiresANameInOneLanguageAtLeast()
    {
        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = " ", QuestionCount = 0 });

        Assert.Equal("Request.Invalid", result.Error.Code);
        await _translator.DidNotReceiveWithAnyArgs().TranslateAsync(default!, default, default, default);
    }

    [Fact]
    public async Task CreateAsync_TranslatesASlovakNameToEnglish()
    {
        _translator.TranslateAsync("Vtáky", Language.Sk, Language.En, Arg.Any<CancellationToken>()).Returns(" Birds ");

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = " Vtáky ", QuestionCount = 0 });

        Assert.True(result.IsSuccess);
        Assert.Equal(("Vtáky", "Birds"), (result.Value.NameSk, result.Value.NameEn));
        await _categories.Received(1).AddAsync(
            Arg.Is<QuestionCategory>(c => c.NameSk == "Vtáky" && c.NameEn == "Birds"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_TranslatesAnEnglishNameToSlovak()
    {
        _translator.TranslateAsync("Trains", Language.En, Language.Sk, Arg.Any<CancellationToken>()).Returns("Vlaky");

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameEn = "Trains", QuestionCount = 0 });

        Assert.True(result.IsSuccess);
        Assert.Equal(("Vlaky", "Trains"), (result.Value.NameSk, result.Value.NameEn));
    }

    [Fact]
    public async Task CreateAsync_RejectsATakenNameBeforeTranslating()
    {
        _categories.GetByNameAsync("Vtáky", Language.Sk, Arg.Any<CancellationToken>()).Returns(TestEntities.Category(1, "Vtáky", "Birds"));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = "Vtáky", QuestionCount = 5 });

        Assert.Equal("QuestionCategory.NameTaken", result.Error.Code);
        await _translator.DidNotReceiveWithAnyArgs().TranslateAsync(default!, default, default, default);
    }

    [Fact]
    public async Task CreateAsync_RejectsATranslationAnotherCategoryHasBeforeGenerating()
    {
        _translator.TranslateAsync("Vtáctvo", Language.Sk, Language.En, Arg.Any<CancellationToken>()).Returns("Birds");
        _categories.GetByNameAsync("Birds", Language.En, Arg.Any<CancellationToken>()).Returns(TestEntities.Category(1, "Vtáky", "Birds"));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = "Vtáctvo", QuestionCount = 5 });

        Assert.Equal("QuestionCategory.TranslationNameTaken", result.Error.Code);
        Assert.Contains("Vtáctvo", result.Error.Message);
        Assert.Contains("Birds", result.Error.Message);
        await _generation.DidNotReceive().GenerateAsync(Arg.Any<QuestionCategory>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("B")]
    [InlineData("A translation that is far too long to be any category name")]
    public async Task CreateAsync_RejectsAnUnusableTranslation(string? translation)
    {
        _translator.TranslateAsync(Arg.Any<string>(), Arg.Any<Language>(), Arg.Any<Language>(), Arg.Any<CancellationToken>())
            .Returns(translation);

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = "Vtáky", QuestionCount = 5 });

        Assert.Equal("QuestionCategory.TranslationFailed", result.Error.Code);
        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
        await _generation.DidNotReceive().GenerateAsync(Arg.Any<QuestionCategory>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_SaysWhenTheTranslatorIsUnavailable()
    {
        _translator.TranslateAsync(Arg.Any<string>(), Arg.Any<Language>(), Arg.Any<Language>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new QuestionGeneratorUnavailableException("down"));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = "Vtáky", QuestionCount = 5 });

        Assert.Equal("QuestionCategory.TranslationUnavailable", result.Error.Code);
        await _categories.DidNotReceive().AddAsync(Arg.Any<QuestionCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ReportsAConflictWhenTheNameWasTakenDuringGeneration()
    {
        _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new UniqueConstraintException("duplicate", new InvalidOperationException()));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { NameSk = "Šport", NameEn = "Sport", QuestionCount = 2 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheCategory()
    {
        var category = TestEntities.Category(1, "Sport");
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _sut.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _categories.Received(1).Remove(category);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFoundForUnknownCategory()
    {
        _categories.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);

        var result = await _sut.DeleteAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task GenerateAiQuestionsAsync_StoresTheQuestionsAndReturnsThem()
    {
        var category = TestEntities.Category(2, "Šport", "Sport");
        _categories.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(category);
        _language.Language = Language.En;

        var result = await _sut.GenerateAiQuestionsAsync(2, new GenerateAiQuestionsRequest { Count = 4 });

        Assert.True(result.IsSuccess);
        Assert.Equal(new(4, 4, 0), result.Value.Summary);
        Assert.Equal(4, result.Value.Questions.Count);
        Assert.All(result.Value.Questions, question =>
        {
            Assert.Equal("Sport", question.CategoryName);
            Assert.Equal("Ai", question.Source);
        });
        await _generation.Received(1).GenerateAsync(category, 4, Arg.Any<CancellationToken>());
        await _questions.Received(1).AddRangeAsync(Arg.Any<IEnumerable<Question>>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAiQuestionsAsync_ReportsUnavailableWhenNothingComesBack()
    {
        _categories.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(TestEntities.Category(2, "Sport"));
        _generation.GenerateAsync(Arg.Any<QuestionCategory>(), 4, Arg.Any<CancellationToken>())
            .Returns(call => Generated(call.Arg<QuestionCategory>(), 4, created: 0));

        var result = await _sut.GenerateAiQuestionsAsync(2, new GenerateAiQuestionsRequest { Count = 4 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAiQuestionsAsync_SaysWhenTheProviderItselfIsUnavailable()
    {
        _categories.GetByIdAsync(2, Arg.Any<CancellationToken>()).Returns(TestEntities.Category(2, "Sport"));
        _generation.GenerateAsync(Arg.Any<QuestionCategory>(), 4, Arg.Any<CancellationToken>())
            .Returns(new QuestionGenerationResult([], 4, 0, ProviderUnavailable: true));

        var result = await _sut.GenerateAiQuestionsAsync(2, new GenerateAiQuestionsRequest { Count = 4 });

        Assert.Equal(ErrorType.Unavailable, result.Error.Type);
        Assert.Equal("QuestionGeneration.ProviderUnavailable", result.Error.Code);
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateAiQuestionsAsync_ReturnsNotFoundForUnknownCategory()
    {
        _categories.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);

        var result = await _sut.GenerateAiQuestionsAsync(7, new GenerateAiQuestionsRequest { Count = 4 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        await _generation.DidNotReceive().GenerateAsync(Arg.Any<QuestionCategory>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAiQuestionsAsync_RemovesOnlyTheAiQuestionsOfTheCategory()
    {
        var category = TestEntities.Category(2, "Sport");
        IReadOnlyList<Question> aiQuestions =
        [
            TestEntities.Question(1, ValidText, "22", category, source: QuestionSource.Ai),
            TestEntities.Question(2, "How long is a marathon in metres?", "42195", category, source: QuestionSource.Ai)
        ];
        _categories.ExistsAsync(2, Arg.Any<CancellationToken>()).Returns(true);
        _questions.FindAsync(2, QuestionSource.Ai, Arg.Any<CancellationToken>()).Returns(aiQuestions);

        var result = await _sut.DeleteAiQuestionsAsync(2);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Deleted);
        _questions.Received(1).RemoveRange(aiQuestions);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAiQuestionsAsync_ReturnsNotFoundForUnknownCategory()
    {
        _categories.ExistsAsync(7, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.DeleteAiQuestionsAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    private static QuestionGenerationResult Generated(QuestionCategory category, int requested, int created, int discarded = 0) =>
        new(
            Enumerable.Range(1, created)
                .Select(n => new Question($"Generated question number {n}?", n.ToString(), category, 3, QuestionSource.Ai, TestEntities.CreatedAt))
                .ToList(),
            requested,
            discarded);
}

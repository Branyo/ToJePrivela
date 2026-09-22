using NSubstitute;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Common;
using ToJePrivela.Application.QuestionCategories;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.QuestionCategories;

public class QuestionCategoryServiceTests
{
    private readonly IQuestionCategoryRepository _categories = Substitute.For<IQuestionCategoryRepository>();
    private readonly IPlayerRepository _players = Substitute.For<IPlayerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly QuestionCategoryService _sut;

    public QuestionCategoryServiceTests()
    {
        _sut = new QuestionCategoryService(_categories, _players, _unitOfWork);
    }

    [Fact]
    public async Task GetAllAsync_MapsEveryCategory()
    {
        _categories.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns([TestEntities.Category(1, "Sport"), TestEntities.Category(2, "History")]);

        var result = await _sut.GetAllAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(["Sport", "History"], result.Value.Select(c => c.Name));
        Assert.All(result.Value, c => Assert.Null(c.AddedByPlayer));
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
    public async Task CreateAsync_StoresTheCategory()
    {
        _categories.GetByNameAsync("Sport", Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);
        _players.ExistsAsync(1, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport", AddedByPlayerId = 1 });

        Assert.True(result.IsSuccess);
        Assert.Equal("Sport", result.Value.Name);
        await _categories.Received(1).AddAsync(Arg.Any<QuestionCategory>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RejectsDuplicateName()
    {
        _categories.GetByNameAsync("Sport", Arg.Any<CancellationToken>()).Returns(TestEntities.Category(1, "Sport"));

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport" });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _categories.DidNotReceive().AddAsync(Arg.Any<QuestionCategory>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownAuthor()
    {
        _categories.GetByNameAsync("Sport", Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);
        _players.ExistsAsync(99, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport", AddedByPlayerId = 99 });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_AllowsMissingAuthor()
    {
        _categories.GetByNameAsync("Sport", Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);

        var result = await _sut.CreateAsync(new CreateQuestionCategoryRequest { Name = "Sport" });

        Assert.True(result.IsSuccess);
        await _players.DidNotReceive().ExistsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_RejectsNameOwnedByAnotherCategory()
    {
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(TestEntities.Category(1, "Sport"));
        _categories.GetByNameAsync("History", Arg.Any<CancellationToken>()).Returns(TestEntities.Category(2, "History"));

        var result = await _sut.UpdateAsync(1, new UpdateQuestionCategoryRequest { Name = "History" });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task UpdateAsync_RenamesTheCategory()
    {
        var category = TestEntities.Category(1, "Sport");
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);
        _categories.GetByNameAsync("Cars", Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);

        var result = await _sut.UpdateAsync(1, new UpdateQuestionCategoryRequest { Name = "Cars" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Cars", category.Name);
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNotFoundForUnknownCategory()
    {
        _categories.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);

        var result = await _sut.UpdateAsync(7, new UpdateQuestionCategoryRequest { Name = "Cars" });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheCategory()
    {
        var category = TestEntities.Category(1, "Sport");
        _categories.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _sut.DeleteAsync(1);

        Assert.True(result.IsSuccess);
        _categories.Received(1).Remove(category);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsNotFoundForUnknownCategory()
    {
        _categories.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((QuestionCategory?)null);

        var result = await _sut.DeleteAsync(7);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}

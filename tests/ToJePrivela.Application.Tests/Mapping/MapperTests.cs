using ToJePrivela.Application.Abstractions.Ai;
using ToJePrivela.Application.Games.Mapping;
using ToJePrivela.Application.Players.Dtos;
using ToJePrivela.Application.Players.Mapping;
using ToJePrivela.Application.QuestionCategories.Dtos;
using ToJePrivela.Application.QuestionCategories.Mapping;
using ToJePrivela.Application.Questions.Dtos;
using ToJePrivela.Application.Questions.Mapping;
using ToJePrivela.Application.Tests.Common;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Mapping;

public class MapperTests
{
    private static readonly DateTime Start = new(2026, 9, 22, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PlayerMapper_CopiesIdAndName()
    {
        var dto = PlayerMapper.ToDto(TestEntities.Player(4, "Brano"));

        Assert.Equal(4, dto.Id);
        Assert.Equal("Brano", dto.Name);
    }

    [Fact]
    public void PlayerMapper_BuildsEntityFromRequest()
    {
        var player = PlayerMapper.ToEntity(new CreatePlayerRequest { Name = " Brano " });

        Assert.Equal("Brano", player.Name);
        Assert.Equal(0, player.Id);
    }

    [Fact]
    public void GameMapper_ToDto_ListsPlayerIds()
    {
        var dto = GameMapper.ToDto(TestEntities.Game(3, [5, 6], Start));

        Assert.Equal(3, dto.Id);
        Assert.Equal(Start, dto.Started);
        Assert.Null(dto.Finished);
        Assert.Equal([5, 6], dto.PlayerIds);
    }

    [Fact]
    public void GameMapper_ToDetailsDto_FallsBackToEmptyNameWhenPlayerIsNotLoaded()
    {
        var details = GameMapper.ToDetailsDto(TestEntities.Game(3, [5, 6], Start));

        Assert.Equal(2, details.Players.Count);
        Assert.All(details.Players, player =>
        {
            Assert.Equal(string.Empty, player.Name);
            Assert.Equal(0, player.BadPoints);
        });
        Assert.Equal([5, 6], details.Players.Select(p => p.PlayerId));
    }

    [Fact]
    public void QuestionCategoryMapper_MapsAuthorWhenLoaded()
    {
        var category = TestEntities.Category(1, "Sport", addedByPlayerId: 2);
        typeof(QuestionCategory).GetProperty(nameof(QuestionCategory.AddedByPlayer))!
            .SetValue(category, TestEntities.Player(2, "Duri"));

        var dto = QuestionCategoryMapper.ToDto(category);

        Assert.Equal("Sport", dto.Name);
        Assert.NotNull(dto.AddedByPlayer);
        Assert.Equal("Duri", dto.AddedByPlayer!.Name);
    }

    [Fact]
    public void QuestionCategoryMapper_LeavesAuthorNullWhenNotLoaded()
    {
        var dto = QuestionCategoryMapper.ToDto(TestEntities.Category(1, "Sport", addedByPlayerId: 2));

        Assert.Null(dto.AddedByPlayer);
    }

    [Fact]
    public void QuestionCategoryMapper_BuildsEntityFromRequest()
    {
        var category = QuestionCategoryMapper.ToEntity(
            new CreateQuestionCategoryRequest { Name = "Sport", AddedByPlayerId = 3 });

        Assert.Equal("Sport", category.Name);
        Assert.Equal(3, category.AddedByPlayerId);
    }

    [Fact]
    public void QuestionMapper_CopiesEveryFieldAndDerivesBadPoints()
    {
        var question = TestEntities.Question(9, "Which year was ChatGPT publicly released?", "2022", "History", 4);

        var dto = QuestionMapper.ToDto(question);

        Assert.Equal(9, dto.Id);
        Assert.Equal("Which year was ChatGPT publicly released?", dto.Text);
        Assert.Equal("2022", dto.Answer);
        Assert.Equal("History", dto.Category);
        Assert.Equal(4, dto.Difficulty);
        Assert.Equal(2, dto.BadPoints);
    }

    [Fact]
    public void QuestionMapper_MapsGeneratedQuestionWithSameBadPointsRule()
    {
        var dto = QuestionMapper.ToDto(new GeneratedQuestion("Some generated question?", "7", "Sport", 5));

        Assert.Equal("7", dto.Answer);
        Assert.Equal(1, dto.BadPoints);
    }

    [Fact]
    public void QuestionMapper_BuildsEntityFromRequest()
    {
        var question = QuestionMapper.ToEntity(new CreateQuestionRequest
        {
            Text = "Which year was ChatGPT publicly released?",
            Answer = "2022",
            Category = "History",
            Difficulty = 3
        });

        Assert.Equal("2022", question.Answer);
        Assert.Equal(3, question.BadPoints);
    }
}

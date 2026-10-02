using ToJePrivela.Application.QuestionGeneration;
using ToJePrivela.Application.Rules;
using ToJePrivela.Application.Rules.Dtos;
using ToJePrivela.Domain.Entities;

namespace ToJePrivela.Application.Tests.Rules;

public class GameRulesServiceTests
{
    private readonly GameRulesDto _rules = new GameRulesService().Get().Value;

    [Fact]
    public void Get_ServesTheLimitsTheDomainEnforces()
    {
        Assert.Equal(new LimitDto(Game.MinPlayers, Game.MaxPlayers), _rules.Players);
        Assert.Equal(new LimitDto(Game.MinBadCardLimit, Game.MaxBadCardLimit), _rules.BadCardLimit);
        Assert.Equal(Game.DefaultBadCardLimit, _rules.DefaultBadCardLimit);
        Assert.Equal(new LimitDto(Question.MinBadPoints, Question.MaxBadPoints), _rules.BadPoints);
        Assert.Equal(new LimitDto(Player.NameMinLength, Player.NameMaxLength), _rules.PlayerName);
        Assert.Equal(new LimitDto(QuestionCategory.NameMinLength, QuestionCategory.NameMaxLength), _rules.CategoryName);
        Assert.Equal(QuestionGenerationOptions.MaxCount, _rules.MaxAiQuestionCount);
    }

    [Fact]
    public void Get_ServesTheAvatarPoolInItsOrder()
    {
        Assert.Equal(PlayerAvatars.All, _rules.Avatars);
    }
}

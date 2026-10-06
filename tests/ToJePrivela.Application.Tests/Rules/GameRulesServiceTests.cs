using ToJePrivela.Application.Accounts;
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
        Assert.Equal(Account.MaxPlayers, _rules.MaxPlayersPerAccount);
        Assert.Equal(new LimitDto(QuestionCategory.NameMinLength, QuestionCategory.NameMaxLength), _rules.CategoryName);
        Assert.Equal(QuestionGenerationOptions.MaxCount, _rules.MaxAiQuestionCount);
        Assert.Equal(new LimitDto(Account.NameMinLength, Account.NameMaxLength), _rules.LoginName);
        Assert.Equal(new LimitDto(PasswordRules.MinLength, PasswordRules.MaxLength), _rules.Password);
    }

    [Fact]
    public void Get_ServesTheAvatarPoolInItsOrder()
    {
        Assert.Equal(PlayerAvatars.All, _rules.Avatars);
    }
}

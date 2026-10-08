using ToJePrivela.Application.Common;
using ToJePrivela.Application.Games.Dtos;
using ToJePrivela.Application.Questions.Dtos;

namespace ToJePrivela.Application.Tests.Common;

public class RequestValidatorTests
{
    [Fact]
    public void Validate_AcceptsAValidRequest()
    {
        Assert.Null(RequestValidator.Validate(new CreateGameRequest { PlayerIds = [1, 2] }));
    }

    [Fact]
    public void Validate_RejectsAMissingRequest()
    {
        var error = RequestValidator.Validate<CreateGameRequest>(null);

        Assert.Equal(ErrorType.Validation, error!.Type);
        Assert.Equal("Request.Missing", error.Code);
    }

    [Fact]
    public void Validate_NamesEveryBrokenRule()
    {
        var error = RequestValidator.Validate(new CreateQuestionRequest { TextSk = "Short", TextEn = "Short", Answer = "4", CategoryId = 0, BadPoints = 9 });

        Assert.Equal("Request.Invalid", error!.Code);
        Assert.Contains("Slovak question text", error.Message);
        Assert.Contains("English question text", error.Message);
        Assert.Contains("Category id", error.Message);
        Assert.Contains("Bad points", error.Message);
    }

    [Fact]
    public void Validate_RejectsWhatTheDomainWouldReject()
    {
        var error = RequestValidator.Validate(new CreateQuestionRequest { TextSk = "      ab      ", TextEn = "Which year was it?", Answer = "eight", CategoryId = 1 });

        Assert.Equal("Request.Invalid", error!.Code);
        Assert.Contains("Slovak question text should have from 8 to 512 characters.", error.Message);
        Assert.Contains("Answer should be a number", error.Message);
    }

    [Fact]
    public void Validate_CountsDifferentPlayersOnly()
    {
        var error = RequestValidator.Validate(new CreateGameRequest { PlayerIds = [1, 1] });

        Assert.Equal("Request.Invalid", error!.Code);
        Assert.Contains("A game needs from 2 to 12 different players.", error.Message);
    }

    [Fact]
    public void Validate_PutsTheLimitsIntoTheMessages()
    {
        var error = RequestValidator.Validate(new CreateGameRequest { PlayerIds = [1, 2], BadCardLimit = 99 });

        Assert.Equal("Bad card limit should be between 2 and 10.", error!.Message);
    }
}

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
        var error = RequestValidator.Validate(new CreateQuestionRequest { Text = "Short", Answer = "4", CategoryId = 0, BadPoints = 9 });

        Assert.Equal("Request.Invalid", error!.Code);
        Assert.Contains("Question text", error.Message);
        Assert.Contains("Category id", error.Message);
        Assert.Contains("Bad points", error.Message);
    }
}

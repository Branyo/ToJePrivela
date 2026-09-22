using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ToJePrivela.Api.Common;
using ToJePrivela.Application.Common;

namespace ToJePrivela.Api.Tests.Common;

public class ResultExtensionsTests
{
    [Fact]
    public void SuccessWithoutValue_BecomesNoContent()
    {
        var response = Result.Success().ToActionResult();

        Assert.IsType<NoContentResult>(response);
    }

    [Fact]
    public void SuccessWithValue_BecomesOk()
    {
        var response = Result.Success("payload").ToActionResult();

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        Assert.Equal("payload", ok.Value);
    }

    [Fact]
    public void SuccessfulCreate_BecomesCreatedAtAction()
    {
        var result = Result.Success(new { Id = 7 });

        var response = result.ToCreatedResult("GetThing", value => new { id = value.Id });

        var created = Assert.IsType<CreatedAtActionResult>(response.Result);
        Assert.Equal("GetThing", created.ActionName);
        Assert.Equal(7, created.RouteValues!["id"]);
        Assert.Equal(result.Value, created.Value);
    }

    [Fact]
    public void FailedCreate_DoesNotReturnCreated()
    {
        var response = Result.Failure<string>(Error.NotFound("Thing.NotFound", "Missing."))
            .ToCreatedResult("GetThing", _ => new { id = 1 });

        var problem = Assert.IsType<ObjectResult>(response.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
    }

    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Unavailable, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(ErrorType.Failure, StatusCodes.Status500InternalServerError)]
    public void EveryErrorTypeMapsToItsStatusCode(ErrorType type, int expectedStatusCode)
    {
        var response = Result.Failure(new Error("Some.Code", "Some message.", type)).ToActionResult();

        var problem = Assert.IsType<ObjectResult>(response);
        Assert.Equal(expectedStatusCode, problem.StatusCode);
    }

    [Fact]
    public void ProblemDetails_CarryMessageAndCode()
    {
        var response = Result.Failure(Error.Conflict("Player.NameTaken", "Player 'Brano' already exists."))
            .ToActionResult();

        var problem = Assert.IsType<ProblemDetails>(Assert.IsType<ObjectResult>(response).Value);
        Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
        Assert.Equal("Conflict", problem.Title);
        Assert.Equal("Player 'Brano' already exists.", problem.Detail);
        Assert.Equal("Player.NameTaken", problem.Extensions["code"]);
    }
}

using ToJePrivela.Application.Common;

namespace ToJePrivela.Application.Tests.Common;

public class ResultTests
{
    private static readonly Error SomeError = Error.NotFound("Some.Code", "Some message.");

    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Success_CarriesTheValue()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Failure_CarriesTheError()
    {
        var result = Result.Failure<int>(SomeError);

        Assert.True(result.IsFailure);
        Assert.Equal(SomeError, result.Error);
    }

    [Fact]
    public void Value_ThrowsOnFailedResult()
    {
        var result = Result.Failure<int>(SomeError);

        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Theory]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Unavailable)]
    [InlineData(ErrorType.Failure)]
    public void Error_KeepsItsType(ErrorType type)
    {
        var error = new Error("Code", "Message", type);

        Assert.Equal(type, Result.Failure(error).Error.Type);
    }
}

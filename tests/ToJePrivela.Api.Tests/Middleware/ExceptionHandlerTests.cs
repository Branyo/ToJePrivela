using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ToJePrivela.Api.Middleware;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Domain.Common;

namespace ToJePrivela.Api.Tests.Middleware;

public class ExceptionHandlerTests
{
    [Fact]
    public async Task DomainException_BecomesABadRequestWithACode()
    {
        var sut = new DomainExceptionHandler(NullLogger<DomainExceptionHandler>.Instance);

        var (handled, status, problem) = await HandleAsync(sut, new DomainException("name must not be empty."));

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, status);
        Assert.Equal(DomainExceptionHandler.Code, problem.GetProperty("code").GetString());
        Assert.Equal("name must not be empty.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ConcurrencyConflict_BecomesAConflictWithACode()
    {
        var sut = new ConcurrencyConflictExceptionHandler(NullLogger<ConcurrencyConflictExceptionHandler>.Instance);

        var (handled, status, problem) = await HandleAsync(
            sut,
            new ConcurrencyConflictException("changed", new InvalidOperationException()));

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal(ConcurrencyConflictExceptionHandler.Code, problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task OtherExceptions_AreLeftToTheNextHandler()
    {
        var sut = new DomainExceptionHandler(NullLogger<DomainExceptionHandler>.Instance);

        Assert.False(await sut.TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException(), CancellationToken.None));
    }

    private static async Task<(bool Handled, int Status, JsonElement Problem)> HandleAsync(
        Microsoft.AspNetCore.Diagnostics.IExceptionHandler handler,
        Exception exception)
    {
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        return (handled, context.Response.StatusCode, problem);
    }
}

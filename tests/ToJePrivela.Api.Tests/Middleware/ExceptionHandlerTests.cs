using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ToJePrivela.Api.Middleware;
using ToJePrivela.Application.Abstractions.Persistence;
using ToJePrivela.Application.Accounts;
using ToJePrivela.Domain.Common;
using ToJePrivela.Identity.Tokens;

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
    public async Task MissingReference_BecomesAConflictWhileTheLoginExists()
    {
        var accounts = Substitute.For<IAccountRepository>();
        accounts.LoginExistsAsync(5, Arg.Any<CancellationToken>()).Returns(true);

        var (handled, status, problem) = await HandleMissingReferenceAsync(accounts, signedInAccountId: 5);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal(MissingReferenceExceptionHandler.Code, problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task MissingReference_SignsOutALoginDeletedDuringTheRequest()
    {
        var accounts = Substitute.For<IAccountRepository>();
        accounts.LoginExistsAsync(5, Arg.Any<CancellationToken>()).Returns(false);

        var (handled, status, problem) = await HandleMissingReferenceAsync(accounts, signedInAccountId: 5);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status401Unauthorized, status);
        Assert.Equal(AccountErrors.UnknownAccount.Code, problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task MissingReference_WithoutASignIn_IsAConflict()
    {
        var accounts = Substitute.For<IAccountRepository>();

        var (_, status, _) = await HandleMissingReferenceAsync(accounts, signedInAccountId: null);

        Assert.Equal(StatusCodes.Status409Conflict, status);
        await accounts.DidNotReceiveWithAnyArgs().LoginExistsAsync(default, default);
    }

    [Fact]
    public async Task MissingReference_StaysAConflictWhenTheLoginCannotBeLookedUp()
    {
        var accounts = Substitute.For<IAccountRepository>();
        accounts.LoginExistsAsync(5, Arg.Any<CancellationToken>())
            .Returns<bool>(_ => throw new InvalidOperationException("database is locked"));

        var (handled, status, problem) = await HandleMissingReferenceAsync(accounts, signedInAccountId: 5);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal(MissingReferenceExceptionHandler.Code, problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ConcurrencyConflict_SignsOutALoginDeletedDuringTheRequest()
    {
        var accounts = Substitute.For<IAccountRepository>();
        accounts.LoginExistsAsync(5, Arg.Any<CancellationToken>()).Returns(false);

        var (handled, status, problem) = await HandleAsync(
            new ConcurrencyConflictExceptionHandler(NullLogger<ConcurrencyConflictExceptionHandler>.Instance),
            new ConcurrencyConflictException("changed", new InvalidOperationException()),
            SignedInContext(accounts, signedInAccountId: 5));

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status401Unauthorized, status);
        Assert.Equal(AccountErrors.UnknownAccount.Code, problem.GetProperty("code").GetString());
    }

    [Fact]
    public async Task OtherExceptions_AreLeftToTheNextHandler()
    {
        var sut = new DomainExceptionHandler(NullLogger<DomainExceptionHandler>.Instance);

        Assert.False(await sut.TryHandleAsync(new DefaultHttpContext(), new InvalidOperationException(), CancellationToken.None));
    }

    private static Task<(bool Handled, int Status, JsonElement Problem)> HandleMissingReferenceAsync(
        IAccountRepository accounts,
        int? signedInAccountId) =>
        HandleAsync(
            new MissingReferenceExceptionHandler(NullLogger<MissingReferenceExceptionHandler>.Instance),
            new MissingReferenceException("gone", new InvalidOperationException()),
            SignedInContext(accounts, signedInAccountId));

    private static DefaultHttpContext SignedInContext(IAccountRepository accounts, int? signedInAccountId)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton(accounts).BuildServiceProvider()
        };

        if (signedInAccountId is { } id)
        {
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(AccessTokenClaims.Subject, id.ToString(CultureInfo.InvariantCulture))],
                authenticationType: "Test"));
        }

        return context;
    }

    private static async Task<(bool Handled, int Status, JsonElement Problem)> HandleAsync(
        Microsoft.AspNetCore.Diagnostics.IExceptionHandler handler,
        Exception exception,
        DefaultHttpContext? context = null)
    {
        context ??= new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        context.Response.Body.Position = 0;
        var problem = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        return (handled, context.Response.StatusCode, problem);
    }
}

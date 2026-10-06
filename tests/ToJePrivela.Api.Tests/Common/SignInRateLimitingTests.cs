using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using ToJePrivela.Api.Common;

namespace ToJePrivela.Api.Tests.Common;

public class SignInRateLimitingTests
{
    [Theory]
    [InlineData("""{ "name": "Brano", "password": "x" }""", "brano")]
    [InlineData("""{ "Name": "  ŠTEFAN " }""", "štefan")]
    [InlineData("""{ "password": "x" }""", "")]
    [InlineData("""{ "name": 42 }""", "")]
    [InlineData("""[ "Brano" ]""", "")]
    [InlineData("""{ "name": """, "")]
    public async Task ReadLoginName_ReturnsTheNameKeyOrNothing(string body, string expected)
    {
        var request = RequestWith(body);

        var name = await SignInRateLimiting.ReadLoginNameAsync(request, CancellationToken.None);

        Assert.Equal(expected, name);
    }

    [Fact]
    public async Task ReadLoginName_LeavesTheBodyForModelBinding()
    {
        const string body = """{ "name": "Brano" }""";
        var request = RequestWith(body);

        await SignInRateLimiting.ReadLoginNameAsync(request, CancellationToken.None);

        Assert.Equal(body, await new StreamReader(request.Body).ReadToEndAsync());
    }

    [Fact]
    public async Task ReadLoginName_SkipsALargeBody()
    {
        var body = $$"""{ "name": "Brano", "padding": "{{new string('x', SignInRateLimiting.MaxReadBodyBytes)}}" }""";

        var name = await SignInRateLimiting.ReadLoginNameAsync(RequestWith(body), CancellationToken.None);

        Assert.Equal(string.Empty, name);
    }

    [Fact]
    public async Task ReadLoginName_SkipsABodyOfUnknownLength()
    {
        var request = RequestWith("""{ "name": "Brano" }""");
        request.ContentLength = null;

        var name = await SignInRateLimiting.ReadLoginNameAsync(request, CancellationToken.None);

        Assert.Equal(string.Empty, name);
    }

    [Fact]
    public void PartitionKey_IsTheAddressAndTheName()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.7");

        Assert.Equal("10.0.0.7|", SignInRateLimiting.PartitionKey(context));
    }

    private static HttpRequest RequestWith(string body)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var context = new DefaultHttpContext();
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Request.ContentType = "application/json";
        return context.Request;
    }
}

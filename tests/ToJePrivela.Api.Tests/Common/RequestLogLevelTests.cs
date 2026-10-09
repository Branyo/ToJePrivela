using Microsoft.AspNetCore.Http;
using Serilog.Events;
using ToJePrivela.Api.Common;

namespace ToJePrivela.Api.Tests.Common;

public class RequestLogLevelTests
{
    [Theory]
    [InlineData(200, false, LogEventLevel.Information)]
    [InlineData(404, false, LogEventLevel.Information)]
    [InlineData(500, false, LogEventLevel.Error)]
    [InlineData(503, false, LogEventLevel.Error)]
    [InlineData(200, true, LogEventLevel.Verbose)]
    [InlineData(429, true, LogEventLevel.Information)]
    [InlineData(503, true, LogEventLevel.Error)]
    public void For_LogsByStatusAndEndpoint(int status, bool probe, LogEventLevel expected) =>
        Assert.Equal(expected, RequestLogLevel.For(Request(status, probe), exception: null));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void For_LogsAnExceptionAsAnError_EvenWhenTheStatusLooksSuccessful(bool probe) =>
        Assert.Equal(LogEventLevel.Error, RequestLogLevel.For(Request(200, probe), new InvalidOperationException()));

    private static HttpContext Request(int status, bool probe)
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = status;
        context.SetEndpoint(new Endpoint(
            requestDelegate: null,
            probe ? new EndpointMetadataCollection(ProbeEndpointMetadata.Instance) : EndpointMetadataCollection.Empty,
            displayName: probe ? "probe" : "other"));
        return context;
    }
}

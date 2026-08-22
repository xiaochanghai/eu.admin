using EU.Core.Extensions.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class FluentResponseBodyMiddleware_Should
{
    [Fact]
    public async Task Preserve_the_response_stream_for_SSE_requests()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var application = new ApplicationBuilder(services);
        Stream? downstreamResponseBody = null;
        application.UseResponseBodyRead();
        application.Run(context =>
        {
            downstreamResponseBody = context.Response.Body;
            return context.Response.WriteAsync("event: message\n\ndata: test\n\n");
        });

        RequestDelegate pipeline = application.Build();
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        context.Request.Headers.Accept = "text/event-stream";

        await pipeline(context);

        Assert.Same(responseBody, downstreamResponseBody);
    }
}

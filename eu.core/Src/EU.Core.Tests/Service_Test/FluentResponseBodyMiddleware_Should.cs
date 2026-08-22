using EU.Core.Extensions.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class FluentResponseBodyMiddleware_Should
{
    [Theory]
    [InlineData("/api/chat/runs")]
    [InlineData("/api/stream/chat/7f3c4d58-9d4d-4fc2-aef3-71c85c4bc661")]
    [InlineData("/api/agents/9a17ab74-b87c-4980-b2e3-2715a40ee942/runs")]
    public async Task Preserve_the_response_stream_for_known_SSE_requests(string path)
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
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = path;

        await pipeline(context);

        Assert.Same(responseBody, downstreamResponseBody);
    }

    [Fact]
    public async Task Buffer_non_streaming_requests_even_when_the_accept_header_is_forged()
    {
        using ServiceProvider services = new ServiceCollection().BuildServiceProvider();
        var application = new ApplicationBuilder(services);
        Stream? downstreamResponseBody = null;
        application.UseResponseBodyRead();
        application.Run(context =>
        {
            downstreamResponseBody = context.Response.Body;
            return context.Response.WriteAsync("{}");
        });

        RequestDelegate pipeline = application.Build();
        var context = new DefaultHttpContext();
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/unrelated";
        context.Request.Headers.Accept = "text/event-stream";

        await pipeline(context);

        Assert.NotSame(responseBody, downstreamResponseBody);
    }
}

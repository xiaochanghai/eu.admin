#nullable enable
using System.Text;
using System.Text.Json;
using EU.Core.Api.Agent.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EU.Core.Tests;

/// <summary>模拟请求日志的 EnableBuffering/ReadToEnd/回卷流程，不写日志或连接外部服务。</summary>
public sealed class AgentRequestBodyLoggingOrderTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Oversized_body_is_rejected_before_full_log_or_endpoint(bool knownLength)
    {
        using var services = Services();
        using var body = new ObservedBody((int)RequestBodyLimitMiddleware.MaximumRequestBodyBytes * 3);
        var context = Context(services, body, knownLength);
        bool logged = false;
        bool executed = false;
        var limit = new RequestBodyLimitMiddleware(async http =>
        {
            await ReadForLogAsync(http);
            logged = true;
            executed = true;
        });
        var errors = new ProblemDetailsMiddleware(limit.InvokeAsync, NullLoggerFactory.Instance);
        await errors.InvokeAsync(context);

        Assert.Equal(413, context.Response.StatusCode);
        Assert.False(logged);
        Assert.False(executed);
        if (knownLength) Assert.Equal(0, body.BytesRead);
        else Assert.InRange(body.BytesRead, 1, body.Size - 1);
        context.Response.Body.Position = 0;
        using var result = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(600002, result.RootElement.GetProperty("Status").GetInt32());
        Assert.Equal("REQUEST_BODY_TOO_LARGE", result.RootElement.GetProperty("Data").GetProperty("ErrorCode").GetString());
        await context.Response.CompleteAsync();
        await context.Request.Body.DisposeAsync();
    }

    [Theory]
    [InlineData("/api/agents", 131072)]
    [InlineData("/api/skills/example", 131073)]
    [InlineData("/api/knowledge-bases/example/documents/pdf", 131073)]
    public async Task Allowed_body_can_be_read_by_log_then_replayed_by_endpoint(string path, int size)
    {
        using var services = Services();
        using var body = new ObservedBody(size);
        var context = Context(services, body, false);
        context.Request.Path = path;
        string? received = null;
        var limit = new RequestBodyLimitMiddleware(async http =>
        {
            await ReadForLogAsync(http);
            using var reader = new StreamReader(http.Request.Body, leaveOpen: true);
            received = await reader.ReadToEndAsync();
        });
        await new ProblemDetailsMiddleware(limit.InvokeAsync, NullLoggerFactory.Instance).InvokeAsync(context);
        Assert.Equal(200, context.Response.StatusCode);
        Assert.Equal(size, received?.Length);
        Assert.Equal(size, body.BytesRead);
        await context.Response.CompleteAsync();
        await context.Request.Body.DisposeAsync();
    }

    private static async Task ReadForLogAsync(HttpContext context)
    {
        // 与 RequRespLogMiddleware 一致，读取完成之后才允许写日志。
        context.Request.EnableBuffering();
        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
        await reader.ReadToEndAsync();
        context.Request.Body.Position = 0;
    }

    private static ServiceProvider Services()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options => options.JsonSerializerOptions.PropertyNamingPolicy = null);
        return services.BuildServiceProvider();
    }

    private static DefaultHttpContext Context(IServiceProvider services, ObservedBody body, bool knownLength)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Method = "POST";
        context.Request.Path = "/api/agents";
        context.Request.ContentType = "application/json";
        context.Request.ContentLength = knownLength ? body.Size : null;
        context.Request.Body = body;
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class ObservedBody(int size) : Stream
    {
        private readonly MemoryStream _inner = new(Encoding.UTF8.GetBytes(new string('a', size)));
        public int Size => size;
        public int BytesRead { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = _inner.Read(buffer, offset, count);
            BytesRead += read;
            return read;
        }
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = await _inner.ReadAsync(buffer, cancellationToken);
            BytesRead += read;
            return read;
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}

using EU.Core.Common.HttpContextUser;
using EU.Core.Extensions.Middlewares;
using EU.Core.Model;
using EU.Core.Model.Entity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Xunit;

namespace EU.Core.Tests.Service_Test;

public sealed class RecordAccessLogsMiddleware_Should
{
    [Fact]
    public void Include_terminal_http_result_fields_in_access_log_payload()
    {
        Type type = typeof(UserAccessModel);

        Assert.NotNull(type.GetProperty("StatusCode"));
        Assert.NotNull(type.GetProperty("Outcome"));
        Assert.NotNull(type.GetProperty("ErrorCode"));
    }

    [Fact]
    public async Task Not_read_request_body_when_capture_request_data_is_disabled()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Middleware:RecordAccessLogs:Enabled"] = "true",
                ["Middleware:RecordAccessLogs:CaptureRequestData"] = "false"
            })
            .Build();
        bool nextInvoked = false;
        var middleware = new RecordAccessLogsMiddleware(
            _ =>
            {
                nextInvoked = true;
                return Task.CompletedTask;
            },
            TestUser.Instance,
            NullLogger<RecordAccessLogsMiddleware>.Instance,
            new TestWebHostEnvironment(),
            configuration);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/chat/runs";
        context.Request.Method = HttpMethods.Post;
        context.Request.Body = new ThrowWhenReadStream();

        await middleware.InvokeAsync(context);

        Assert.True(nextInvoked);
    }

    private sealed class TestUser : IUser
    {
        public static readonly TestUser Instance = new();
        public string Name => "operator";
        public Guid? ID => null;
        public SmUsers UserInfo => new();
        public Guid? CompanyId => null;
        public Guid? GroupId => null;
        public long TenantId => 0;
        public long? SessionId => null;
        public ServiceResult<string> MessageModel { get; set; } = new();
        public bool IsAuthenticated() => true;
        public IEnumerable<Claim> GetClaimsIdentity() => [];
        public List<string> GetClaimValueByType(string claimType) => [];
        public string GetToken() => string.Empty;
        public string GetPlatform() => string.Empty;
        public List<string> GetUserInfoFromToken(string claimType) => [];
    }

    private sealed class ThrowWhenReadStream : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => 0;
        public override long Position { get => 0; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("The request body must not be read.");
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => ValueTask.FromException<int>(new InvalidOperationException("The request body must not be read."));
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "EU.Core.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Testing";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

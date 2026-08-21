namespace EU.Core.Api.Agent.Errors;

using EU.Core.IServices.Knowledge;

public sealed class RequestBodyTooLargeException : Exception;

public sealed class RequestBodyLimitMiddleware(RequestDelegate next)
{
    public const long MaximumRequestBodyBytes = 131_072;
    public const long MaximumSkillRequestBodyBytes = 2_129_920;
    public const long MaximumKnowledgePdfRequestBodyBytes =
        EU.Core.Services.AgKnowledgeBaseDefinitionServices.MaximumPdfBytes + 65_536;

    public async Task InvokeAsync(HttpContext context)
    {
        if (HasRequestBody(context.Request) &&
            context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            long maximumBytes = GetMaximumRequestBodyBytes(context.Request.Path);
            if (context.Request.ContentLength > maximumBytes)
            {
                throw new RequestBodyTooLargeException();
            }

            context.Request.Body = new BoundedReadStream(context.Request.Body, maximumBytes);
        }

        await next(context);
    }

    private static bool HasRequestBody(HttpRequest request) =>
        request.Method is "POST" or "PUT" or "PATCH";

    private static long GetMaximumRequestBodyBytes(PathString path)
    {
        if (path.StartsWithSegments("/api/skills", StringComparison.OrdinalIgnoreCase))
        {
            return MaximumSkillRequestBodyBytes;
        }

        if (path.StartsWithSegments("/api/knowledge-bases", StringComparison.OrdinalIgnoreCase)
            && path.Value?.EndsWith("/documents/pdf", StringComparison.OrdinalIgnoreCase) is true)
        {
            return MaximumKnowledgePdfRequestBodyBytes;
        }

        return MaximumRequestBodyBytes;
    }

    private sealed class BoundedReadStream(Stream inner, long maximumBytes) : Stream
    {
        private long _bytesRead;

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => _bytesRead;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = inner.Read(buffer, offset, count);
            Count(read);
            return read;
        }

        public override int Read(Span<byte> buffer)
        {
            int read = inner.Read(buffer);
            Count(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            int read = await inner.ReadAsync(buffer, cancellationToken);
            Count(read);
            return read;
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken) =>
            ReadArrayAsync(buffer, offset, count, cancellationToken);

        private async Task<int> ReadArrayAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken)
        {
            int read = await inner.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            Count(read);
            return read;
        }

        private void Count(int read)
        {
            _bytesRead += read;
            if (_bytesRead > maximumBytes)
            {
                throw new RequestBodyTooLargeException();
            }
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

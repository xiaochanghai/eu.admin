using Microsoft.AspNetCore.Http;

namespace EU.Core.Extensions.Middlewares;

internal static class StreamingRequestPolicy
{
    public static bool IsKnownEventStreamRequest(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method))
        {
            return false;
        }

        string path = request.Path.Value ?? string.Empty;
        return path.StartsWith("/api/stream/chat/", StringComparison.OrdinalIgnoreCase) ||
            path.Equals("/api/chat/runs", StringComparison.OrdinalIgnoreCase) ||
            IsAgentRunPath(path);
    }

    private static bool IsAgentRunPath(string path)
    {
        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 4 &&
            string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(segments[1], "agents", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(segments[3], "runs", StringComparison.OrdinalIgnoreCase);
    }
}

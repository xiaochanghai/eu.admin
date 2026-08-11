namespace EU.Core.Agent.Api.Security;

public static class AgentWorkloadClassifier
{
    public static bool SupportsIdempotency(HttpRequest request) =>
        IsExpensive(request)
        && !request.Path.Equals("/api/chat/runs", StringComparison.OrdinalIgnoreCase);

    public static bool IsExpensive(HttpRequest request)
    {
        if (!HttpMethods.IsPost(request.Method)) return false;

        PathString path = request.Path;
        return path.Equals("/api/chat/runs", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/api/evaluation-batches", StringComparison.OrdinalIgnoreCase)
            || (path.StartsWithSegments(
                    "/api/orchestrations",
                    StringComparison.OrdinalIgnoreCase)
                && path.Value?.EndsWith("/runs", StringComparison.OrdinalIgnoreCase) == true)
            || (path.StartsWithSegments(
                    "/api/evaluation-batches",
                    StringComparison.OrdinalIgnoreCase)
                && path.Value?.EndsWith("/model-judge", StringComparison.OrdinalIgnoreCase) == true);
    }
}

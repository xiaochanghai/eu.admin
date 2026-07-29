namespace EU.Core.Agent.Runtime;

public sealed record AgentRuntimeOptions(
    Uri ModelEndpoint,
    string ModelCredentialAlias,
    TimeSpan ModelTimeout,
    TimeSpan ToolCallTimeout);

public interface IModelCredentialResolver
{
    ValueTask<string?> ResolveAsync(
        string credentialAlias,
        CancellationToken cancellationToken = default);
}

public sealed class EnvironmentAndDotEnvModelCredentialResolver(
    string contentRoot,
    bool allowDotEnv = true) : IModelCredentialResolver
{
    public ValueTask<string?> ResolveAsync(
        string credentialAlias,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string aliasName = credentialAlias.StartsWith("alias:", StringComparison.Ordinal)
            ? credentialAlias["alias:".Length..]
            : credentialAlias;
        string aliasVariable = "AGENT_MODEL_CREDENTIAL_" + aliasName
            .ToUpperInvariant()
            .Replace('-', '_')
            .Replace('.', '_');
        string? value = Environment.GetEnvironmentVariable(aliasVariable) ??
                        Environment.GetEnvironmentVariable("AGENT_MODEL_API_KEY");
        if (!string.IsNullOrWhiteSpace(value))
        {
            return ValueTask.FromResult<string?>(value);
        }

        if (!allowDotEnv)
        {
            return ValueTask.FromResult<string?>(null);
        }

        string? path = FindNearestDotEnv(contentRoot);
        if (path is null)
        {
            return ValueTask.FromResult<string?>(null);
        }

        foreach (string line in File.ReadLines(path))
        {
            cancellationToken.ThrowIfCancellationRequested();
            int equals = line.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            string key = line[..equals].Trim();
            if (!string.Equals(key, aliasVariable, StringComparison.Ordinal) &&
                !string.Equals(key, "AGENT_MODEL_API_KEY", StringComparison.Ordinal))
            {
                continue;
            }

            string candidate = line[(equals + 1)..].Trim();
            if (candidate.Length >= 2 &&
                ((candidate[0] == '"' && candidate[^1] == '"') ||
                 (candidate[0] == '\'' && candidate[^1] == '\'')))
            {
                candidate = candidate[1..^1];
            }

            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return ValueTask.FromResult<string?>(candidate);
            }
        }

        return ValueTask.FromResult<string?>(null);
    }

    private static string? FindNearestDotEnv(string startDirectory)
    {
        DirectoryInfo? directory = new(Path.GetFullPath(startDirectory));
        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, ".env");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }
}

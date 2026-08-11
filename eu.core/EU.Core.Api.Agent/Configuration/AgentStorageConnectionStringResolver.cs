using Microsoft.Extensions.Configuration;

namespace EU.Core.Api.Agent.Configuration;

public interface IAgentStorageConnectionStringResolver
{
    string? Resolve(string alias);
}

public sealed class EnvironmentAndDotEnvAgentStorageConnectionStringResolver(
    string contentRoot,
    bool allowDotEnv,
    IConfiguration? configuration = null) : IAgentStorageConnectionStringResolver
{
    public string? Resolve(string alias)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        string variable = ToEnvironmentVariable(alias);
        string? value = configuration?[variable] ??
                        configuration?["AGENT_STORAGE_CONNECTION_STRING"] ??
                        configuration?["EUCORE_AGENT_SQLSERVER"] ??
                        Environment.GetEnvironmentVariable(variable) ??
                        Environment.GetEnvironmentVariable(
                            "AGENT_STORAGE_CONNECTION_STRING") ??
                        Environment.GetEnvironmentVariable(
                            "EUCORE_AGENT_SQLSERVER");
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (!allowDotEnv)
        {
            return null;
        }

        string? path = FindNearestDotEnv(contentRoot);
        if (path is null)
        {
            return null;
        }

        foreach (string line in File.ReadLines(path))
        {
            int equals = line.IndexOf('=');
            if (equals <= 0)
            {
                continue;
            }

            string key = line[..equals].Trim();
            if (!string.Equals(key, variable, StringComparison.Ordinal) &&
                !string.Equals(
                    key,
                    "AGENT_STORAGE_CONNECTION_STRING",
                    StringComparison.Ordinal) &&
                !string.Equals(
                    key,
                    "EUCORE_AGENT_SQLSERVER",
                    StringComparison.Ordinal))
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

            return string.IsNullOrWhiteSpace(candidate) ? null : candidate;
        }

        return null;
    }

    internal static string ToEnvironmentVariable(string alias)
    {
        string name = alias.StartsWith("alias:", StringComparison.Ordinal)
            ? alias["alias:".Length..]
            : alias;
        return "AGENT_STORAGE_CONNECTION_" + name
            .ToUpperInvariant()
            .Replace('-', '_')
            .Replace('.', '_');
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

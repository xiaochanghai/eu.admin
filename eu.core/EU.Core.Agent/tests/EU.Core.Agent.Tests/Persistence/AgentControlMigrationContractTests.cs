using System.Text.RegularExpressions;
using Xunit;

namespace EU.Core.Agent.Tests.Persistence;

public sealed class AgentControlMigrationContractTests
{
    [Theory]
    [InlineData("0002_agent_control.sql")]
    [InlineData("down/0002_agent_control.down.sql")]
    public void P2_migrations_are_comment_only_operator_owned_placeholders(string relativePath)
    {
        string[] pathSegments = ["database", "migrations", .. relativePath.Split('/')];
        string path = FindSolutionFilePath(pathSegments);
        string sql = File.ReadAllText(path);
        string executableContent = Regex.Replace(sql, @"--[^\r\n]*|/\*.*?\*/|\s", string.Empty, RegexOptions.Singleline);

        Assert.Contains("operator-owned placeholder", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(executableContent);
        Assert.DoesNotMatch(@"(?i)(create|alter|drop|insert|update|delete|select|exec|merge|table|column)", sql);
    }

    private static string FindSolutionFilePath(params string[] segments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the formal solution root.");
    }
}

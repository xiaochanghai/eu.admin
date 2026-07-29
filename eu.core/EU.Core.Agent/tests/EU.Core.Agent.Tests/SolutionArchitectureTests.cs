using System.Xml.Linq;
using Xunit;

namespace EU.Core.Agent.Tests;

public sealed class SolutionArchitectureTests
{
    private static readonly string SolutionRoot = FindSolutionRoot();
    private static readonly IReadOnlyDictionary<string, string> FormalProjectMap = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["src/EU.Core.Agent.Api/EU.Core.Agent.Api.csproj"] = "EU.Core.Agent.Api",
        ["src/EU.Core.Agent.Application/EU.Core.Agent.Application.csproj"] = "EU.Core.Agent.Application",
        ["src/EU.Core.Agent.Runtime/EU.Core.Agent.Runtime.csproj"] = "EU.Core.Agent.Runtime",
        ["src/EU.Core.Agent.Infrastructure/EU.Core.Agent.Infrastructure.csproj"] = "EU.Core.Agent.Infrastructure",
        ["tests/EU.Core.Agent.Tests/EU.Core.Agent.Tests.csproj"] = "EU.Core.Agent.Tests",
    };

    public static IEnumerable<object[]> FormalProjects()
    {
        return FormalProjectMap.Select(project => new object[] { project.Key, project.Value });
    }

    [Theory]
    [MemberData(nameof(FormalProjects))]
    public void Formal_project_exists_at_its_solution_path(string relativePath, string assemblyName)
    {
        string projectPath = Path.Combine(SolutionRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.True(File.Exists(projectPath), $"Expected formal project '{assemblyName}' at '{projectPath}'.");
    }

    [Fact]
    public void Formal_project_references_follow_the_independent_layering_contract()
    {
        var expectedReferences = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["EU.Core.Agent.Api"] = ["EU.Core.Agent.Application", "EU.Core.Agent.Infrastructure", "EU.Core.Agent.Runtime"],
            ["EU.Core.Agent.Application"] = [],
            ["EU.Core.Agent.Infrastructure"] = ["EU.Core.Agent.Application"],
            ["EU.Core.Agent.Runtime"] = ["EU.Core.Agent.Application"],
            ["EU.Core.Agent.Tests"] = ["EU.Core.Agent.Api", "EU.Core.Agent.Application", "EU.Core.Agent.Infrastructure", "EU.Core.Agent.Runtime"],
        };

        foreach ((string relativePath, string projectName) in FormalProjectMap)
        {
            string projectPath = Path.GetFullPath(Path.Combine(SolutionRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            Assert.True(File.Exists(projectPath), $"Expected formal project '{projectName}' at '{projectPath}'.");

            string[] actualReferences = ReadProjectReferences(projectPath)
                .Select(reference => Path.GetFileNameWithoutExtension(reference))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expectedReferences[projectName].OrderBy(name => name, StringComparer.Ordinal), actualReferences);
        }
    }

    [Fact]
    public void Formal_project_references_resolve_only_to_projects_inside_the_agent_container()
    {
        string solutionRootWithSeparator = SolutionRoot.EndsWith(Path.DirectorySeparatorChar)
            ? SolutionRoot
            : SolutionRoot + Path.DirectorySeparatorChar;
        var formalProjectPaths = FormalProjectMap.Keys
            .Select(relativePath => Path.GetFullPath(Path.Combine(
                SolutionRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar))))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach ((string relativePath, string projectName) in FormalProjectMap)
        {
            string projectPath = Path.GetFullPath(Path.Combine(SolutionRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            Assert.True(File.Exists(projectPath), $"Expected formal project '{projectName}' at '{projectPath}'.");

            foreach (string referencedProjectPath in ReadProjectReferences(projectPath))
            {
                Assert.StartsWith(solutionRootWithSeparator, referencedProjectPath, StringComparison.OrdinalIgnoreCase);
                Assert.Contains(referencedProjectPath, formalProjectPaths);
            }
        }
    }

    [Fact]
    public void Application_remains_a_framework_and_provider_independent_contract_project()
    {
        string applicationProjectPath = Path.Combine(SolutionRoot, "src", "EU.Core.Agent.Application", "EU.Core.Agent.Application.csproj");
        Assert.True(File.Exists(applicationProjectPath), $"Expected application project at '{applicationProjectPath}'.");

        XDocument project = XDocument.Load(applicationProjectPath);
        string[] packageReferences = project
            .Descendants("PackageReference")
            .Select(element => (string?)element.Attribute("Include") ?? string.Empty)
            .ToArray();

        Assert.Empty(packageReferences);
    }

    private static IReadOnlyCollection<string> ReadProjectReferences(string projectPath)
    {
        string projectDirectory = Path.GetDirectoryName(projectPath)!;
        return XDocument.Load(projectPath)
            .Descendants("ProjectReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(include => !string.IsNullOrWhiteSpace(include))
            .Select(include => Path.GetFullPath(Path.Combine(projectDirectory, include!)))
            .ToArray();
    }

    private static string FindSolutionRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")) &&
                File.Exists(Path.Combine(directory.FullName, "Directory.Packages.props")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Could not locate the EU.Core.Agent solution root.");
    }
}

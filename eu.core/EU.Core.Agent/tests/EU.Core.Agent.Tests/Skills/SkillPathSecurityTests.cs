using EU.Core.Agent.Application.Skills;
using EU.Core.Agent.Infrastructure.Skills;
using Xunit;

namespace EU.Core.Agent.Tests.Skills;

public sealed class SkillPathSecurityTests
{
    [Theory]
    [InlineData("../outside.md")]
    [InlineData("references/../../outside.md")]
    [InlineData("C:\\outside.md")]
    [InlineData("/outside.md")]
    [InlineData("other/file.md")]
    [InlineData("root.md")]
    public async Task Store_rejects_paths_outside_the_controlled_layout(string path)
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            var store = new ControlledSkillFileStore(directory);
            await store.EnsureDraftAsync("safe-skill", "Safe", "Safe skill");

            SkillFileStoreException exception = await Assert.ThrowsAsync<SkillFileStoreException>(
                () => store.WriteDraftTextAsync("safe-skill", path, "content"));

            Assert.Equal(SkillErrorCodes.PathInvalid, exception.Code);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Store_rejects_text_larger_than_two_mebibytes()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            var store = new ControlledSkillFileStore(directory);
            await store.EnsureDraftAsync("safe-skill", "Safe", "Safe skill");

            SkillFileStoreException exception = await Assert.ThrowsAsync<SkillFileStoreException>(
                () => store.WriteDraftTextAsync(
                    "safe-skill",
                    "references/large.md",
                    new string('x', ControlledSkillFileStore.MaxTextFileBytes + 1)));

            Assert.Equal(SkillErrorCodes.FileTooLarge, exception.Code);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Store_supports_controlled_folders_but_rejects_symbolic_links()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            var store = new ControlledSkillFileStore(directory);
            await store.EnsureDraftAsync("safe-skill", "Safe", "Safe skill");
            await store.WriteDraftTextAsync("safe-skill", "scripts/example.py", "print('stored only')");
            Assert.Equal(
                "print('stored only')",
                await store.ReadDraftTextAsync("safe-skill", "scripts/example.py"));

            string outside = Path.Combine(directory, "outside");
            Directory.CreateDirectory(outside);
            string link = Path.Combine(directory, "safe-skill", "draft", "references");
            try
            {
                Directory.CreateSymbolicLink(link, outside);
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (PlatformNotSupportedException)
            {
                return;
            }

            SkillFileStoreException exception = await Assert.ThrowsAsync<SkillFileStoreException>(
                () => store.WriteDraftTextAsync(
                    "safe-skill",
                    "references/escape.md",
                    "blocked"));
            Assert.Equal(SkillErrorCodes.PathInvalid, exception.Code);
            Assert.False(File.Exists(Path.Combine(outside, "escape.md")));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"eu-core-agent-skill-path-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}

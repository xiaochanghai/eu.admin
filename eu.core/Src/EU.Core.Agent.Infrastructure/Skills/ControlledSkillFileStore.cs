using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using EU.Core.Agent.Application.Skills;

namespace EU.Core.Agent.Infrastructure.Skills;

public sealed class ControlledSkillFileStore : ISkillFileStore, IPublishedSkillContentStore
{
    public const int MaxTextFileBytes = 2 * 1024 * 1024;

    private static readonly HashSet<string> AllowedTopLevelDirectories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "references",
            "assets",
            "tests",
            "scripts"
        };

    private static readonly UTF8Encoding Utf8 = new(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);

    private readonly string _root;

    public ControlledSkillFileStore(string rootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPath);
        _root = Path.GetFullPath(rootPath);
        RejectAncestorReparsePoints(_root);
        Directory.CreateDirectory(_root);
        RejectAncestorReparsePoints(_root);
    }

    public async Task ProbeReadinessAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RejectAncestorReparsePoints(_root);
        string probePath = Path.Combine(
            _root,
            $".readiness-{Guid.NewGuid():N}.tmp");
        byte[] expected = RandomNumberGenerator.GetBytes(16);
        try
        {
            await using (var stream = new FileStream(
                probePath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous))
            {
                await stream.WriteAsync(expected, cancellationToken);
                await stream.FlushAsync(cancellationToken);
                stream.Position = 0;
                var actual = new byte[expected.Length];
                await stream.ReadExactlyAsync(actual, cancellationToken);
                if (!CryptographicOperations.FixedTimeEquals(expected, actual))
                {
                    throw new IOException(
                        "The controlled Skill storage readiness probe could not be verified.");
                }

                RejectAncestorReparsePoints(_root);
            }
        }
        finally
        {
            if (File.Exists(probePath))
            {
                File.Delete(probePath);
            }
        }
    }

    public async Task EnsureDraftAsync(
        string skillCode,
        string name,
        string description,
        CancellationToken cancellationToken = default)
    {
        string draftRoot = DraftRoot(skillCode, create: true);
        string skillFile = ResolveDraftPath(skillCode, "SKILL.md", createParent: true);
        if (File.Exists(skillFile))
        {
            return;
        }

        string safeName = string.IsNullOrWhiteSpace(name) ? skillCode : name.Trim();
        string safeDescription = string.IsNullOrWhiteSpace(description)
            ? $"Reusable capability for {safeName}."
            : description.Trim().Replace("\r", " ").Replace("\n", " ");
        string content =
            $"""
            ---
            name: {skillCode}
            description: {safeDescription}
            ---

            # {safeName}

            Describe the Skill instructions, references, inputs, outputs, and constraints here.
            """;
        await AtomicWriteAsync(skillFile, content, cancellationToken);
        RejectReparsePoint(draftRoot);
    }

    public Task<IReadOnlyList<SkillFileEntry>> ListDraftAsync(
        string skillCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string root = DraftRoot(skillCode, create: true);
        RejectTreeReparsePoints(root);
        IReadOnlyList<SkillFileEntry> entries = SkillContractCloner.ReadOnly(
            Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => new SkillFileEntry(
                    NormalizeRelative(root, path),
                    new FileInfo(path).Length))
                .OrderBy(entry => entry.Path, StringComparer.Ordinal));
        return Task.FromResult(entries);
    }

    public async Task<string> ReadDraftTextAsync(
        string skillCode,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        string path = ResolveDraftPath(skillCode, relativePath, createParent: false);
        if (!File.Exists(path))
        {
            throw Error(SkillErrorCodes.FileMissing, "The Skill file was not found.");
        }

        RejectReparsePoint(path);
        var info = new FileInfo(path);
        if (info.Length > MaxTextFileBytes)
        {
            throw Error(SkillErrorCodes.FileTooLarge, "The Skill text file exceeds 2 MiB.");
        }

        byte[] bytes = await File.ReadAllBytesAsync(path, cancellationToken);
        try
        {
            return Utf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw Error(SkillErrorCodes.PathInvalid, "The Skill text file must be valid UTF-8.");
        }
    }

    public async Task WriteDraftTextAsync(
        string skillCode,
        string relativePath,
        string content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        byte[] bytes = Utf8.GetBytes(content);
        if (bytes.Length > MaxTextFileBytes)
        {
            throw Error(SkillErrorCodes.FileTooLarge, "The Skill text file exceeds 2 MiB.");
        }

        string path = ResolveDraftPath(skillCode, relativePath, createParent: true);
        if (File.Exists(path))
        {
            RejectReparsePoint(path);
        }

        await AtomicWriteAsync(path, content, cancellationToken);
    }

    public async Task<SkillPublishArtifact> PublishAsync(
        string skillCode,
        string versionLabel,
        CancellationToken cancellationToken = default)
    {
        string draftRoot = DraftRoot(skillCode, create: false);
        string skillMd = ResolveDraftPath(skillCode, "SKILL.md", createParent: false);
        if (!File.Exists(skillMd))
        {
            throw Error(SkillErrorCodes.PublishInvalid, "SKILL.md is required before publish.");
        }

        string skillText = await ReadDraftTextAsync(skillCode, "SKILL.md", cancellationToken);
        if (!HasRequiredFrontMatter(skillText))
        {
            throw Error(
                SkillErrorCodes.PublishInvalid,
                "SKILL.md must start with YAML front matter containing name and description.");
        }

        RejectTreeReparsePoints(draftRoot);
        string versionsRoot = Path.Combine(SkillRoot(skillCode, create: true), "versions");
        Directory.CreateDirectory(versionsRoot);
        RejectReparsePoint(versionsRoot);
        string destination = ResolveVersionPath(versionsRoot, versionLabel);
        if (Directory.Exists(destination))
        {
            throw Error(SkillErrorCodes.VersionConflict, "The Skill version directory already exists.");
        }

        string temporary = Path.Combine(versionsRoot, $".publishing-{Guid.NewGuid():N}");
        Directory.CreateDirectory(temporary);
        try
        {
            foreach (string source in Directory.EnumerateFiles(draftRoot, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                RejectReparsePoint(source);
                string relative = NormalizeRelative(draftRoot, source);
                string target = Path.GetFullPath(Path.Combine(temporary, relative.Replace('/', Path.DirectorySeparatorChar)));
                EnsureContained(temporary, target);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(source, target, overwrite: false);
            }

            HashedSkillTree tree = await HashTreeAsync(
                temporary,
                captureSkillInstructions: false,
                cancellationToken);
            string manifest = ManifestHash(tree.Files);
            Directory.Move(temporary, destination);
            SetTreeReadOnly(destination);
            return new SkillPublishArtifact(versionLabel, manifest, tree.Files);
        }
        catch
        {
            if (Directory.Exists(temporary))
            {
                Directory.Delete(temporary, recursive: true);
            }

            throw;
        }
    }

    public Task DeletePublishedAsync(
        string skillCode,
        string versionLabel,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string versionsRoot = Path.Combine(SkillRoot(skillCode, create: false), "versions");
        string path = ResolveVersionPath(versionsRoot, versionLabel);
        if (Directory.Exists(path))
        {
            ClearTreeReadOnly(path);
            Directory.Delete(path, recursive: true);
        }

        return Task.CompletedTask;
    }

    public async Task<PublishedSkillContent?> ReadAsync(
        PublishedSkillReference reference,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        cancellationToken.ThrowIfCancellationRequested();

        string skillRoot = SkillRoot(reference.SkillCode, create: false);
        string versionsRoot = Path.Combine(skillRoot, "versions");
        if (!Directory.Exists(versionsRoot))
        {
            return null;
        }

        RejectReparsePoint(versionsRoot);
        string versionRoot = ResolveVersionPath(versionsRoot, reference.VersionLabel);
        if (!Directory.Exists(versionRoot))
        {
            return null;
        }

        string skillFile = Path.GetFullPath(Path.Combine(versionRoot, "SKILL.md"));
        EnsureContained(versionRoot, skillFile);
        if (!File.Exists(skillFile))
        {
            return null;
        }

        HashedSkillTree tree = await HashTreeAsync(
            versionRoot,
            captureSkillInstructions: true,
            cancellationToken);
        string manifest = ManifestHash(tree.Files);
        if (!SecureHashEquals(manifest, reference.ManifestSha256))
        {
            throw Error(
                SkillErrorCodes.VersionNotPublished,
                "The published Skill artifact no longer matches its manifest.");
        }

        if (tree.SkillInstructionsBytes is not byte[] bytes)
        {
            throw Error(
                SkillErrorCodes.FileMissing,
                "The published SKILL.md could not be read.");
        }

        string instructions;
        try
        {
            instructions = Utf8.GetString(bytes);
        }
        catch (DecoderFallbackException)
        {
            throw Error(
                SkillErrorCodes.PathInvalid,
                "The published SKILL.md must be valid UTF-8.");
        }

        return new PublishedSkillContent(
            reference.VersionId,
            reference.SkillCode,
            reference.VersionLabel,
            manifest,
            instructions)
        {
            SkillName = reference.SkillName
        };
    }

    private string SkillRoot(string skillCode, bool create)
    {
        if (string.IsNullOrWhiteSpace(skillCode) ||
            skillCode.Any(character =>
                !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
        {
            throw Error(SkillErrorCodes.PathInvalid, "The Skill code is not safe for storage.");
        }

        string path = Path.GetFullPath(Path.Combine(_root, skillCode));
        EnsureContained(_root, path);
        if (create)
        {
            Directory.CreateDirectory(path);
        }

        if (Directory.Exists(path))
        {
            RejectReparsePoint(path);
        }

        return path;
    }

    private string DraftRoot(string skillCode, bool create)
    {
        string path = Path.Combine(SkillRoot(skillCode, create), "draft");
        if (create)
        {
            Directory.CreateDirectory(path);
        }

        if (!Directory.Exists(path))
        {
            throw Error(SkillErrorCodes.FileMissing, "The Skill Draft directory was not found.");
        }

        RejectReparsePoint(path);
        return path;
    }

    private string ResolveDraftPath(string skillCode, string relativePath, bool createParent)
    {
        string normalized = ValidateRelativePath(relativePath);
        string root = DraftRoot(skillCode, create: createParent);
        string path = Path.GetFullPath(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        EnsureContained(root, path);
        string? parent = Path.GetDirectoryName(path);
        if (createParent && parent is not null)
        {
            Directory.CreateDirectory(parent);
        }

        RejectExistingParents(root, parent ?? root);
        return path;
    }

    private static string ValidateRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            Path.IsPathRooted(relativePath) ||
            relativePath.StartsWith('/') ||
            relativePath.StartsWith('\\'))
        {
            throw Error(SkillErrorCodes.PathInvalid, "Skill file paths must be relative.");
        }

        string normalized = relativePath.Replace('\\', '/');
        string[] segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 ||
            segments.Any(segment =>
                segment is "." or ".." ||
                segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
        {
            throw Error(SkillErrorCodes.PathInvalid, "The Skill file path is invalid.");
        }

        if (segments.Length == 1)
        {
            if (!string.Equals(segments[0], "SKILL.md", StringComparison.OrdinalIgnoreCase))
            {
                throw Error(
                    SkillErrorCodes.PathInvalid,
                    "Root-level Skill files are limited to SKILL.md.");
            }
        }
        else if (!AllowedTopLevelDirectories.Contains(segments[0]))
        {
            throw Error(
                SkillErrorCodes.PathInvalid,
                "Skill files must be under references, assets, tests, or scripts.");
        }

        return string.Join('/', segments);
    }

    private static string ResolveVersionPath(string versionsRoot, string versionLabel)
    {
        if (string.IsNullOrWhiteSpace(versionLabel) ||
            versionLabel.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            versionLabel.Contains('/') ||
            versionLabel.Contains('\\'))
        {
            throw Error(SkillErrorCodes.VersionInvalid, "The Skill version path is invalid.");
        }

        string path = Path.GetFullPath(Path.Combine(versionsRoot, versionLabel));
        EnsureContained(versionsRoot, path);
        return path;
    }

    private static async Task AtomicWriteAsync(
        string path,
        string content,
        CancellationToken cancellationToken)
    {
        string parent = Path.GetDirectoryName(path)!;
        RejectReparsePoint(parent);
        string temporary = Path.Combine(
            parent,
            $".{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllTextAsync(temporary, content, Utf8, cancellationToken);
            RejectReparsePoint(parent);
            File.Move(temporary, path, overwrite: true);
            RejectReparsePoint(parent);
            RejectReparsePoint(path);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private static async Task<HashedSkillTree> HashTreeAsync(
        string root,
        bool captureSkillInstructions,
        CancellationToken cancellationToken)
    {
        string normalizedRoot = Path.GetFullPath(root);
        RejectReparsePoint(normalizedRoot);
        IReadOnlyList<ControlledFileEntry> entries = EnumerateControlledFiles(
                normalizedRoot)
            .Select(path => new ControlledFileEntry(
                path,
                NormalizeRelative(normalizedRoot, path)))
            .OrderBy(entry => entry.RelativePath, StringComparer.Ordinal)
            .ToArray();
        ValidateCanonicalTreePaths(entries);
        var files = new List<SkillFileHash>();
        byte[]? skillInstructionsBytes = null;
        foreach (ControlledFileEntry entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            bool capture = captureSkillInstructions &&
                string.Equals(
                    entry.RelativePath,
                    "SKILL.md",
                    StringComparison.Ordinal);
            HashedFile hashed = await HashFileAsync(
                normalizedRoot,
                entry.Path,
                capture,
                cancellationToken);
            files.Add(new SkillFileHash(
                entry.RelativePath,
                hashed.Size,
                hashed.Sha256));
            if (capture)
            {
                skillInstructionsBytes = hashed.Bytes;
            }
        }

        return new HashedSkillTree(
            SkillContractCloner.ReadOnly(files),
            skillInstructionsBytes);
    }

    private static void ValidateCanonicalTreePaths(
        IReadOnlyList<ControlledFileEntry> entries)
    {
        var normalizedPaths = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        bool hasCanonicalInstructions = false;
        foreach (ControlledFileEntry entry in entries)
        {
            if (!normalizedPaths.Add(entry.RelativePath))
            {
                throw Error(
                    SkillErrorCodes.PathInvalid,
                    "Published Skill paths must be unique regardless of casing.");
            }

            if (string.Equals(
                    entry.RelativePath,
                    "SKILL.md",
                    StringComparison.Ordinal))
            {
                hasCanonicalInstructions = true;
            }
            else if (string.Equals(
                         entry.RelativePath,
                         "SKILL.md",
                         StringComparison.OrdinalIgnoreCase))
            {
                throw Error(
                    SkillErrorCodes.PathInvalid,
                    "The published instruction file must be named exactly SKILL.md.");
            }
        }

        if (!hasCanonicalInstructions)
        {
            throw Error(
                SkillErrorCodes.FileMissing,
                "The published instruction file must be named exactly SKILL.md.");
        }
    }

    private static IReadOnlyList<string> EnumerateControlledFiles(string root)
    {
        var files = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out string? directory))
        {
            EnsureContained(root, directory);
            if (string.Equals(
                    directory,
                    root,
                    StringComparison.OrdinalIgnoreCase))
            {
                RejectReparsePoint(directory);
            }
            else
            {
                RejectExistingParents(
                    root,
                    Path.GetDirectoryName(directory) ?? root);
            }

            FileAttributes directoryAttributes = File.GetAttributes(directory);
            RejectReparsePoint(directory, directoryAttributes);
            if ((directoryAttributes & FileAttributes.Directory) == 0)
            {
                throw Error(
                    SkillErrorCodes.PathInvalid,
                    "A Skill tree entry changed while it was being verified.");
            }

            foreach (string entry in Directory.EnumerateFileSystemEntries(directory))
            {
                string path = Path.GetFullPath(entry);
                EnsureContained(root, path);
                FileAttributes attributes = File.GetAttributes(path);
                RejectReparsePoint(path, attributes);
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    pending.Push(path);
                }
                else
                {
                    files.Add(path);
                }
            }
        }

        return files;
    }

    private static async Task<HashedFile> HashFileAsync(
        string root,
        string path,
        bool captureBytes,
        CancellationToken cancellationToken)
    {
        string parent = Path.GetDirectoryName(path)!;
        RejectExistingParents(root, parent);
        RejectReparsePoint(path);
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81_920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        RejectExistingParents(root, parent);
        RejectReparsePoint(path);
        if (stream.Length > MaxTextFileBytes)
        {
            throw FileTooLarge();
        }

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using MemoryStream? captured = captureBytes
            ? new MemoryStream(capacity: (int)stream.Length)
            : null;
        byte[] buffer = ArrayPool<byte>.Shared.Rent(81_920);
        long total = 0;
        try
        {
            while (true)
            {
                int maximumRead = (int)Math.Min(
                    buffer.Length,
                    MaxTextFileBytes - total + 1);
                int read = await stream.ReadAsync(
                    buffer.AsMemory(0, maximumRead),
                    cancellationToken);
                if (read == 0)
                {
                    break;
                }

                total += read;
                if (total > MaxTextFileBytes)
                {
                    throw FileTooLarge();
                }

                hash.AppendData(buffer, 0, read);
                captured?.Write(buffer, 0, read);
            }

            RejectExistingParents(root, parent);
            RejectReparsePoint(path);
            return new HashedFile(
                total,
                Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant(),
                captured?.ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static string ManifestHash(IReadOnlyList<SkillFileHash> files)
    {
        string manifest = string.Join(
            '\n',
            files.Select(file => $"{file.Path}\t{file.Size}\t{file.Sha256}"));
        return Convert.ToHexString(SHA256.HashData(Utf8.GetBytes(manifest))).ToLowerInvariant();
    }

    private static bool SecureHashEquals(string actual, string expected)
    {
        if (actual.Length != expected.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Utf8.GetBytes(actual),
            Utf8.GetBytes(expected));
    }

    private static bool HasRequiredFrontMatter(string content)
    {
        string normalized = content.Replace("\r\n", "\n");
        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
        {
            return false;
        }

        int end = normalized.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
        {
            return false;
        }

        string frontMatter = normalized[4..end];
        return frontMatter.Split('\n').Any(line => line.TrimStart().StartsWith("name:", StringComparison.OrdinalIgnoreCase)) &&
               frontMatter.Split('\n').Any(line => line.TrimStart().StartsWith("description:", StringComparison.OrdinalIgnoreCase));
    }

    private static void RejectExistingParents(string root, string path)
    {
        EnsureContained(root, path);
        string current = root;
        RejectReparsePoint(current);
        string relative = Path.GetRelativePath(root, path);
        foreach (string segment in relative.Split(
                     Path.DirectorySeparatorChar,
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (Directory.Exists(current) || File.Exists(current))
            {
                RejectReparsePoint(current);
            }
        }
    }

    private static void RejectAncestorReparsePoints(string path)
    {
        DirectoryInfo? current = new(Path.GetFullPath(path));
        while (current is not null)
        {
            if (current.Exists || File.Exists(current.FullName))
            {
                RejectReparsePoint(current.FullName);
            }

            current = current.Parent;
        }
    }

    private static void RejectTreeReparsePoints(string root)
    {
        RejectReparsePoint(root);
        foreach (string path in Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories))
        {
            RejectReparsePoint(path);
        }
    }

    private static void RejectReparsePoint(string path)
    {
        RejectReparsePoint(path, File.GetAttributes(path));
    }

    private static void RejectReparsePoint(
        string path,
        FileAttributes attributes)
    {
        if ((attributes & FileAttributes.ReparsePoint) != 0)
        {
            throw Error(SkillErrorCodes.PathInvalid, "Symbolic links and reparse points are not allowed in Skill storage.");
        }
    }

    private static void EnsureContained(string root, string path)
    {
        string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
        string normalizedPath = Path.GetFullPath(path);
        if (!string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase) &&
            !normalizedPath.StartsWith(
                normalizedRoot + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            throw Error(SkillErrorCodes.PathInvalid, "The Skill path escapes its controlled directory.");
        }
    }

    private static string NormalizeRelative(string root, string path) =>
        Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');

    private static void SetTreeReadOnly(string root)
    {
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, File.GetAttributes(file) | FileAttributes.ReadOnly);
        }
    }

    private static void ClearTreeReadOnly(string root)
    {
        foreach (string file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
        }
    }

    private static SkillFileStoreException Error(string code, string message) => new(code, message);

    private static SkillFileStoreException FileTooLarge() =>
        Error(
            SkillErrorCodes.FileTooLarge,
            "Each published Skill file must be at most 2 MiB.");

    private sealed record HashedSkillTree(
        IReadOnlyList<SkillFileHash> Files,
        byte[]? SkillInstructionsBytes);

    private sealed record HashedFile(
        long Size,
        string Sha256,
        byte[]? Bytes);

    private sealed record ControlledFileEntry(
        string Path,
        string RelativePath);
}

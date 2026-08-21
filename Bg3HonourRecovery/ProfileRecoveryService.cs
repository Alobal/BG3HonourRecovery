using LSLib.LS;
using LSLib.LS.Enums;
using System.Text.RegularExpressions;

namespace Bg3HonourRecovery;

public sealed class ProfileRecoveryService
{
    private const string DisabledNodeName = "DisabledSingleSaveSessions";
    private const string GuidAttributeName = "Object";
    private static readonly Regex GuidPattern = new(
        @"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static string DefaultProfilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Larian Studios",
        "Baldur's Gate 3",
        "PlayerProfiles",
        "Public",
        "profile8.lsf");

    public ProfileScanResult Analyze(string profilePath)
    {
        ValidateProfilePath(profilePath);

        var fullPath = Path.GetFullPath(profilePath);
        var originalBytes = ReadProfileBytes(fullPath);
        var resource = LoadProfile(originalBytes);
        var imagePaths = FindHonourModeImages(profilePath);
        var sessions = FindDisabledEntries(resource)
            .GroupBy(entry => entry.Guid, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DisabledSession(
                group.Key,
                group.Count(),
                imagePaths.GetValueOrDefault(group.Key)
                ?? imagePaths.GetValueOrDefault(SwapGuidBytePairs(group.Key))))
            .OrderBy(session => session.Guid, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var file = new FileInfo(profilePath);

        return new ProfileScanResult(
            file.FullName,
            sessions,
            file.LastWriteTime,
            file.Length);
    }

    public RecoveryResult Recover(
        string profilePath,
        IEnumerable<string> selectedGuids,
        bool createBackup = true)
    {
        ValidateProfilePath(profilePath);

        var selected = selectedGuids
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(NormalizeGuid)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (selected.Count == 0)
        {
            throw new InvalidOperationException("请至少选择一个要恢复的 GUID。");
        }

        var fullPath = Path.GetFullPath(profilePath);
        var originalBytes = ReadProfileBytes(fullPath);
        var resource = LoadProfile(originalBytes);
        var entries = FindDisabledEntries(resource);
        var available = entries
            .GroupBy(entry => entry.Guid, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        var missing = selected.Where(guid => !available.ContainsKey(guid)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                "文件在扫描后发生了变化，请重新加载。未找到：" + string.Join(", ", missing));
        }

        var removedCount = 0;
        foreach (var entry in entries.Where(entry => selected.Contains(entry.Guid)))
        {
            if (entry.Siblings.Remove(entry.Node))
            {
                removedCount++;
            }
        }

        var expectedRemaining = entries
            .Where(entry => !selected.Contains(entry.Guid))
            .GroupBy(entry => entry.Guid, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

        PruneEmptyDisabledContainers(resource);

        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("无法确定 profile8.lsf 所在目录。");
        var backupPath = createBackup ? CreateUniqueBackupPath(fullPath) : null;
        var temporaryPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.recovery-{Guid.NewGuid():N}.tmp");
        var rollbackPath = Path.Combine(
            directory,
            $".{Path.GetFileName(fullPath)}.rollback-{Guid.NewGuid():N}.tmp");

        if (backupPath is not null)
        {
            EnsureOriginalUnchanged(fullPath, originalBytes);
            File.WriteAllBytes(backupPath, originalBytes);
        }

        try
        {
            SaveProfile(resource, temporaryPath);
            VerifyRecoveredFile(temporaryPath, selected, expectedRemaining, removedCount);
            EnsureOriginalUnchanged(fullPath, originalBytes);
            ReplaceOriginal(temporaryPath, fullPath, rollbackPath);

            try
            {
                VerifyRecoveredFile(fullPath, selected, expectedRemaining, removedCount);
            }
            catch
            {
                try
                {
                    ReplaceOriginal(rollbackPath, fullPath);
                }
                catch (Exception restoreException)
                {
                    throw new InvalidDataException(
                        $"写回后的校验失败，自动回滚也失败。回滚文件保留在：{rollbackPath}",
                        restoreException);
                }

                throw new InvalidDataException("写回后的校验失败，已自动恢复原文件。", null);
            }

            TryDelete(rollbackPath);
        }
        catch
        {
            TryDelete(temporaryPath);
            throw;
        }

        return new RecoveryResult(
            fullPath,
            backupPath,
            removedCount,
            selected.Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static Resource LoadProfile(string path)
    {
        try
        {
            var loadParameters = ResourceLoadParameters.FromGameVersion(Game.BaldursGate3);
            return ResourceUtils.LoadResource(path, ResourceFormat.LSF, loadParameters);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException("无法读取文件。请关闭《博德之门 3》以及可能占用该文件的同步程序。", exception);
        }
        catch (Exception exception)
        {
            throw new InvalidDataException("所选文件不是受支持的 BG3 profile8.lsf，或文件已经损坏。", exception);
        }
    }

    private static Resource LoadProfile(byte[] data)
    {
        try
        {
            using var stream = new MemoryStream(data, writable: false);
            var loadParameters = ResourceLoadParameters.FromGameVersion(Game.BaldursGate3);
            return ResourceUtils.LoadResource(stream, ResourceFormat.LSF, loadParameters);
        }
        catch (Exception exception)
        {
            throw new InvalidDataException("所选文件不是受支持的 BG3 profile8.lsf，或文件已经损坏。", exception);
        }
    }

    private static byte[] ReadProfileBytes(string path)
    {
        try
        {
            return File.ReadAllBytes(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException("无法读取文件。请关闭《博德之门 3》以及可能占用该文件的同步程序。", exception);
        }
    }

    private static void EnsureOriginalUnchanged(string path, byte[] originalBytes)
    {
        var currentBytes = ReadProfileBytes(path);
        if (!currentBytes.AsSpan().SequenceEqual(originalBytes))
        {
            throw new InvalidOperationException("文件在处理期间发生了变化，请重新加载后再试。");
        }
    }

    private static void SaveProfile(Resource resource, string path)
    {
        var conversionParameters = ResourceConversionParameters.FromGameVersion(Game.BaldursGate3);
        ResourceUtils.SaveResource(resource, path, ResourceFormat.LSF, conversionParameters);
    }

    private static List<DisabledEntryRef> FindDisabledEntries(Resource resource)
    {
        var result = new List<DisabledEntryRef>();
        foreach (var region in resource.Regions.Values)
        {
            Visit(region, result);
        }

        return result;
    }

    private static void Visit(Node node, ICollection<DisabledEntryRef> result)
    {
        foreach (var pair in node.Children)
        {
            foreach (var child in pair.Value)
            {
                if (string.Equals(child.Name, DisabledNodeName, StringComparison.Ordinal)
                    && TryReadGuid(child, out var guid))
                {
                    result.Add(new DisabledEntryRef(child, pair.Value, guid));
                }

                Visit(child, result);
            }
        }
    }

    private static bool TryReadGuid(Node node, out string guid)
    {
        guid = string.Empty;
        if (!node.Attributes.TryGetValue(GuidAttributeName, out var attribute))
        {
            return false;
        }

        if (attribute.Value is Guid value)
        {
            guid = value.ToString("D");
            return true;
        }

        try
        {
            value = attribute.AsGuid();
            return SetNormalizedGuid(value, out guid);
        }
        catch
        {
            return Guid.TryParse(Convert.ToString(attribute.Value), out value)
                && SetNormalizedGuid(value, out guid);
        }
    }

    private static bool SetNormalizedGuid(Guid value, out string guid)
    {
        guid = value.ToString("D");
        return true;
    }

    private static void PruneEmptyDisabledContainers(Resource resource)
    {
        foreach (var region in resource.Regions.Values)
        {
            Prune(region);
        }
    }

    private static void Prune(Node node)
    {
        foreach (var key in node.Children.Keys.ToArray())
        {
            var siblings = node.Children[key];
            foreach (var child in siblings.ToArray())
            {
                Prune(child);
                if (string.Equals(child.Name, DisabledNodeName, StringComparison.Ordinal)
                    && !child.Attributes.ContainsKey(GuidAttributeName)
                    && !ContainsDisabledEntry(child))
                {
                    siblings.Remove(child);
                }
            }

            if (siblings.Count == 0)
            {
                node.Children.Remove(key);
            }
        }
    }

    private static bool ContainsDisabledEntry(Node node)
    {
        foreach (var children in node.Children.Values)
        {
            foreach (var child in children)
            {
                if (string.Equals(child.Name, DisabledNodeName, StringComparison.Ordinal)
                    && child.Attributes.ContainsKey(GuidAttributeName))
                {
                    return true;
                }

                if (ContainsDisabledEntry(child))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void VerifyRecoveredFile(
        string path,
        ISet<string> selected,
        IReadOnlyDictionary<string, int> expectedRemaining,
        int expectedRemovedCount)
    {
        var reloaded = LoadProfile(path);
        var remaining = FindDisabledEntries(reloaded);
        if (remaining.Any(entry => selected.Contains(entry.Guid)))
        {
            throw new InvalidDataException("输出文件校验失败：仍然包含已选择的 GUID。");
        }

        if (expectedRemovedCount <= 0)
        {
            throw new InvalidDataException("输出文件校验失败：没有删除任何记录。");
        }
        var actualRemaining = remaining
            .GroupBy(entry => entry.Guid, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        if (actualRemaining.Count != expectedRemaining.Count
            || expectedRemaining.Any(pair => !actualRemaining.TryGetValue(pair.Key, out var count)
                                              || count != pair.Value))
        {
            throw new InvalidDataException("输出文件校验失败：未选择的 GUID 发生了变化。");
        }
    }

    private static void ReplaceOriginal(
        string temporaryPath,
        string originalPath,
        string? rollbackPath = null)
    {
        try
        {
            File.Replace(temporaryPath, originalPath, rollbackPath);
        }
        catch (PlatformNotSupportedException)
        {
            if (rollbackPath is not null)
            {
                File.Copy(originalPath, rollbackPath, overwrite: false);
            }
            File.Move(temporaryPath, originalPath, overwrite: true);
        }
    }

    private static string CreateUniqueBackupPath(string profilePath)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var candidate = $"{profilePath}.backup-{stamp}";
        var suffix = 1;
        while (File.Exists(candidate))
        {
            candidate = $"{profilePath}.backup-{stamp}-{suffix++}";
        }

        return candidate;
    }

    private static void ValidateProfilePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("请选择 profile8.lsf 文件。", nameof(path));
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("找不到所选的 profile8.lsf 文件。", path);
        }

        if (!string.Equals(Path.GetExtension(path), ".lsf", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("请选择扩展名为 .lsf 的文件。");
        }
    }

    private static string NormalizeGuid(string value)
    {
        return Guid.TryParse(value, out var guid)
            ? guid.ToString("D")
            : throw new InvalidDataException($"无效的 GUID：{value}");
    }

    private static Dictionary<string, string> FindHonourModeImages(string profilePath)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var root in GetSaveRoots(profilePath).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            try
            {
                foreach (var imagePath in Directory.EnumerateFiles(
                             root,
                             "HonourMode.WebP",
                             SearchOption.AllDirectories))
                {
                    var directoryName = Path.GetFileName(Path.GetDirectoryName(imagePath)) ?? string.Empty;
                    var match = GuidPattern.Match(directoryName);
                    if (match.Success && Guid.TryParse(match.Value, out var guid))
                    {
                        result.TryAdd(guid.ToString("D"), imagePath);
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // 图片不是恢复所必需的数据；目录不可读时只跳过预览。
            }
        }

        return result;
    }

    private static IEnumerable<string> GetSaveRoots(string profilePath)
    {
        var profileDirectory = Path.GetDirectoryName(Path.GetFullPath(profilePath));
        if (profileDirectory is not null)
        {
            yield return Path.Combine(profileDirectory, "Savegames");
        }

        var defaultDirectory = Path.GetDirectoryName(DefaultProfilePath);
        if (defaultDirectory is not null)
        {
            yield return Path.Combine(defaultDirectory, "Savegames");
        }
    }

    private static string SwapGuidBytePairs(string value)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            return value;
        }

        var parts = guid.ToString("D").Split('-');
        parts[3] = SwapAdjacentBytes(parts[3]);
        parts[4] = SwapAdjacentBytes(parts[4]);
        return string.Join('-', parts);
    }

    private static string SwapAdjacentBytes(string hex)
    {
        var result = new char[hex.Length];
        for (var index = 0; index < hex.Length; index += 4)
        {
            result[index] = hex[index + 2];
            result[index + 1] = hex[index + 3];
            result[index + 2] = hex[index];
            result[index + 3] = hex[index + 1];
        }

        return new string(result);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // 临时文件清理失败不应覆盖真正的恢复错误。
        }
    }

    private sealed record DisabledEntryRef(Node Node, List<Node> Siblings, string Guid);
}

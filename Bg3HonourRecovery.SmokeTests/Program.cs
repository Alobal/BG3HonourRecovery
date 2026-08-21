using Bg3HonourRecovery;
using LSLib.LS;
using LSLib.LS.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

if (args is ["--inspect", var inspectedProfile])
{
    var inspected = new ProfileRecoveryService().Analyze(inspectedProfile);
    foreach (var session in inspected.Sessions)
    {
        if (session.ImagePath is null)
        {
            Console.WriteLine($"{session.Guid}\t<none>");
            continue;
        }

        using var thumbnail = HonourModeImageLoader.LoadThumbnail(session.ImagePath, 160, 90);
        Console.WriteLine($"{session.Guid}\t{session.ImagePath}\t{thumbnail.Width}x{thumbnail.Height}");
    }

    return;
}

if (args is ["--render-list", var renderedProfile, var outputPath])
{
    var scan = new ProfileRecoveryService().Analyze(renderedProfile);
    using var list = new SessionListControl { Size = new System.Drawing.Size(1000, 560) };
    list.SetSessions(scan.Sessions);
    list.CreateControl();
    list.PerformLayout();
    using var bitmap = new System.Drawing.Bitmap(list.Width, list.Height);
    list.DrawToBitmap(bitmap, list.ClientRectangle);
    bitmap.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
    Console.WriteLine(outputPath);
    return;
}

var testRoot = Path.Combine(AppContext.BaseDirectory, "smoke-data");
if (Directory.Exists(testRoot))
{
    Directory.Delete(testRoot, recursive: true);
}
Directory.CreateDirectory(testRoot);

var firstGuid = Guid.Parse("11111111-2222-3333-4444-555555555555");
var secondGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
var byteSwappedGuid = Guid.Parse("6b8209b5-098e-e6a8-4808-5626df10c1c0");
var byteSwappedFolderGuid = Guid.Parse("6b8209b5-098e-e6a8-0848-265610dfc0c1");
var profilePath = Path.Combine(testRoot, "profile8.lsf");
var previewDirectory = Path.Combine(
    testRoot,
    "Savegames",
    "Story",
    $"{firstGuid:D}__HonourMode");
Directory.CreateDirectory(previewDirectory);
var previewPath = Path.Combine(previewDirectory, "HonourMode.WebP");
using (var preview = new SixLabors.ImageSharp.Image<Rgba32>(64, 36))
{
    preview.SaveAsWebp(previewPath);
}
var swappedPreviewDirectory = Path.Combine(
    testRoot,
    "Savegames",
    $"{byteSwappedFolderGuid:D}__HonourMode");
Directory.CreateDirectory(swappedPreviewDirectory);
var swappedPreviewPath = Path.Combine(swappedPreviewDirectory, "HonourMode.WebP");
File.Copy(previewPath, swappedPreviewPath);
CreateSyntheticProfile(profilePath, firstGuid, secondGuid, byteSwappedGuid);
var originalBytes = File.ReadAllBytes(profilePath);

var service = new ProfileRecoveryService();
var initial = service.Analyze(profilePath);
if (initial.Sessions.Count != 3)
{
    var debugResource = ResourceUtils.LoadResource(
        profilePath,
        ResourceFormat.LSF,
        ResourceLoadParameters.FromGameVersion(Game.BaldursGate3));
    foreach (var region in debugResource.Regions.Values)
    {
        DumpNode(region, 0);
    }
}
Assert(initial.Sessions.Count == 3, "应扫描到三个 GUID");
Assert(initial.Sessions.Any(session => session.Guid == firstGuid.ToString("D")), "缺少第一个 GUID");
Assert(initial.Sessions.Any(session => session.Guid == secondGuid.ToString("D")), "缺少第二个 GUID");
Assert(initial.Sessions.Single(session => session.Guid == firstGuid.ToString("D")).ImagePath == previewPath,
    "应按 GUID 关联 HonourMode.WebP");
Assert(initial.Sessions.Single(session => session.Guid == secondGuid.ToString("D")).ImagePath is null,
    "不存在的 HonourMode.WebP 应保持为空");
Assert(initial.Sessions.Single(session => session.Guid == byteSwappedGuid.ToString("D")).ImagePath == swappedPreviewPath,
    "应匹配最后八字节交换后的 GUID 目录");
using (var thumbnail = HonourModeImageLoader.LoadThumbnail(previewPath, 160, 90))
{
    Assert(thumbnail.Size == new System.Drawing.Size(160, 90), "WebP 缩略图尺寸错误");
}

var noBackupDirectory = Path.Combine(testRoot, "no-backup");
Directory.CreateDirectory(noBackupDirectory);
var noBackupProfile = Path.Combine(noBackupDirectory, "profile8.lsf");
CreateSyntheticProfile(noBackupProfile, firstGuid);
var noBackupResult = service.Recover(noBackupProfile, [firstGuid.ToString("D")], createBackup: false);
Assert(noBackupResult.BackupPath is null, "关闭自动备份时不应返回备份路径");
Assert(!Directory.EnumerateFiles(noBackupDirectory).Any(path => path.Contains(".backup-")),
    "关闭自动备份时不应生成持久备份文件");
Assert(!Directory.EnumerateFiles(noBackupDirectory).Any(path => path.Contains(".rollback-")),
    "成功写回后应清理临时回滚文件");

var partial = service.Recover(profilePath, [firstGuid.ToString("D")]);
Assert(partial.RemovedEntries == 1, "定向恢复应只删除一条记录");
Assert(File.Exists(partial.BackupPath), "应创建备份文件");
Assert(File.ReadAllBytes(partial.BackupPath!).SequenceEqual(originalBytes), "备份必须与原文件完全一致");

var afterPartial = service.Analyze(profilePath);
Assert(afterPartial.Sessions.Count == 2, "定向恢复后应保留两条记录");
Assert(afterPartial.Sessions.Any(session => session.Guid == secondGuid.ToString("D")), "未选择的 GUID 不应被删除");
Assert(afterPartial.Sessions.Any(session => session.Guid == byteSwappedGuid.ToString("D")), "字节交换 GUID 不应被删除");

var all = service.Recover(profilePath, [secondGuid.ToString("D"), byteSwappedGuid.ToString("D")]);
Assert(all.RemovedEntries == 2, "第二次恢复应删除剩余记录");
Assert(service.Analyze(profilePath).Sessions.Count == 0, "全部恢复后不应残留失败记录");

var cleanBytes = File.ReadAllBytes(profilePath);
var missingRejected = false;
try
{
    service.Recover(profilePath, [Guid.NewGuid().ToString("D")]);
}
catch (InvalidOperationException)
{
    missingRejected = true;
}
Assert(missingRejected, "不存在的 GUID 必须被拒绝");
Assert(File.ReadAllBytes(profilePath).SequenceEqual(cleanBytes), "失败操作不得修改原文件");

using (var form = new MainForm(service))
{
    Assert(form.Text.Contains("荣誉模式存档恢复器"), "GUI 主窗体应能正常构造");
    Assert(form.MinimumSize.Width >= 960, "GUI 应保持宽松的最小布局宽度");
    var sessionList = FindControl<SessionListControl>(form);
    Assert(sessionList is not null, "GUI 应包含 GUID 列表");
    sessionList!.SetSessions(initial.Sessions);
    form.PerformLayout();
    Assert(sessionList.ItemCount == 3, "自绘列表应显示全部 GUID");
    var largeCheckBox = FindControl<ModernCheckBox>(sessionList);
    Assert(largeCheckBox?.Size == new System.Drawing.Size(32, 32), "复选框应为 32x32");
    var backupCheckBox = FindControls<ModernCheckBox>(form)
        .Single(checkBox => checkBox.AccessibleName == "自动备份");
    Assert(backupCheckBox.Checked, "自动备份应默认开启");
    var clickableThumbnail = FindControl<ThumbnailControl>(sessionList);
    Assert(clickableThumbnail?.Cursor == Cursors.Hand, "有图片的缩略图应显示手型光标");
    Assert(FindControl<ModernScrollBar>(sessionList) is not null, "列表应使用自绘滚动条");
}

Console.WriteLine("PASS: GUID 图片关联、WebP 解码、恢复、备份、失败保护及 GUI 构造均通过。");

static void CreateSyntheticProfile(string path, params Guid[] guids)
{
    var resource = new Resource
    {
        Metadata = new LSMetadata
        {
            MajorVersion = LSMetadata.CurrentMajorVersion,
            MinorVersion = 0,
            Revision = 0,
            BuildNumber = 0,
            Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        },
        MetadataFormat = LSFMetadataFormat.KeysAndAdjacency
    };

    var region = new LSLib.LS.Region
    {
        RegionName = "UserProfiles",
        Name = "UserProfiles"
    };
    resource.Regions.Add(region.RegionName, region);

    var root = new Node { Name = "UserProfiles" };
    Append(region, root);
    var container = new Node { Name = "DisabledSingleSaveSessions" };
    Append(root, container);

    foreach (var guid in guids)
    {
        var entry = new Node { Name = "DisabledSingleSaveSessions" };
        entry.Attributes.Add("Object", new NodeAttribute(AttributeType.UUID) { Value = guid });
        Append(container, entry);
    }

    var parameters = ResourceConversionParameters.FromGameVersion(Game.BaldursGate3);
    ResourceUtils.SaveResource(resource, path, ResourceFormat.LSF, parameters);
}

static void Append(Node parent, Node child)
{
    child.Parent = parent;
    parent.AppendChild(child);
}

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException("TEST FAILED: " + message);
    }
}

static T? FindControl<T>(Control root) where T : Control
{
    foreach (Control child in root.Controls)
    {
        if (child is T match)
        {
            return match;
        }

        var nested = FindControl<T>(child);
        if (nested is not null)
        {
            return nested;
        }
    }

    return null;
}

static IEnumerable<T> FindControls<T>(Control root) where T : Control
{
    foreach (Control child in root.Controls)
    {
        if (child is T match)
        {
            yield return match;
        }

        foreach (var nested in FindControls<T>(child))
        {
            yield return nested;
        }
    }
}

static void DumpNode(Node node, int depth)
{
    Console.WriteLine($"{new string(' ', depth * 2)}{node.Name} [{string.Join(", ", node.Attributes.Select(pair => $"{pair.Key}:{pair.Value.Type}/{pair.Value.Value?.GetType().Name}"))}]");
    foreach (var children in node.Children.Values)
    {
        foreach (var child in children)
        {
            DumpNode(child, depth + 1);
        }
    }
}

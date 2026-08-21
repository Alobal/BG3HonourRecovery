using LSLib.LS;
using LSLib.LS.Enums;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Bg3HonourRecovery;

internal static class RuntimeVerifier
{
    public static int Run()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"bg3-honour-runtime-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "profile8.lsf");
        try
        {
            Directory.CreateDirectory(directory);
            var expected = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
            var previewPath = Path.Combine(directory, "HonourMode.WebP");
            using (var preview = new SixLabors.ImageSharp.Image<Rgba32>(64, 36))
            {
                preview.SaveAsWebp(previewPath);
            }
            using var thumbnail = HonourModeImageLoader.LoadThumbnail(previewPath, 160, 90);
            var resource = CreateResource(expected);
            var conversion = ResourceConversionParameters.FromGameVersion(Game.BaldursGate3);
            ResourceUtils.SaveResource(resource, path, ResourceFormat.LSF, conversion);

            var scan = new ProfileRecoveryService().Analyze(path);
            return scan.Sessions.Count == 1
                   && scan.Sessions[0].Guid == expected.ToString("D")
                   && thumbnail.Width == 160
                   && thumbnail.Height == 90
                ? 0
                : 2;
        }
        catch
        {
            return 1;
        }
        finally
        {
            try
            {
                Directory.Delete(directory, recursive: true);
            }
            catch
            {
                // 运行时校验只关心打包依赖是否可用。
            }
        }
    }

    private static Resource CreateResource(Guid guid)
    {
        var resource = new Resource
        {
            MetadataFormat = LSFMetadataFormat.KeysAndAdjacency
        };
        var region = new LSLib.LS.Region { Name = "UserProfiles", RegionName = "UserProfiles" };
        resource.Regions.Add(region.RegionName, region);

        var container = new Node { Name = "DisabledSingleSaveSessions", Parent = region };
        region.AppendChild(container);
        var entry = new Node { Name = "DisabledSingleSaveSessions", Parent = container };
        entry.Attributes.Add("Object", new NodeAttribute(AttributeType.UUID) { Value = guid });
        container.AppendChild(entry);
        return resource;
    }
}

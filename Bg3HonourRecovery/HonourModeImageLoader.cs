using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Bg3HonourRecovery;

internal static class HonourModeImageLoader
{
    public static Bitmap LoadThumbnail(string path, int width, int height)
    {
        using var image = SixLabors.ImageSharp.Image.Load(path);
        image.Mutate(context => context.Resize(new ResizeOptions
        {
            Size = new SixLabors.ImageSharp.Size(width, height),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center,
            Sampler = KnownResamplers.Lanczos3
        }));

        using var png = new MemoryStream();
        image.SaveAsPng(png);
        png.Position = 0;
        using var decoded = new Bitmap(png);
        return new Bitmap(decoded);
    }
}

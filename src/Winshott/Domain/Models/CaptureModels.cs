namespace Winshott.Domain.Models;

public readonly record struct PixelRect(int X, int Y, int Width, int Height);

public sealed record DisplayInfo(string Id, string Name, PixelRect Bounds, double ScaleFactor);

public sealed record WindowInfo(string Id, string Title, PixelRect Bounds);

public sealed record CapturedImage(byte[] Data, int Width, int Height, string MediaType = "image/png");

public sealed record OcrWord(string Text, PixelRect Bounds, double Confidence);

public sealed record OcrResult(IReadOnlyList<OcrWord> Words, string PlainText);

public sealed record CodeResult(string Text, string Format, PixelRect Bounds, string Status);

public enum ImageOperation
{
    Crop,
    Resize,
    Rotate,
    Blur,
    Pixelate,
    Compose,
    Stitch
}

public sealed record ImageProcessingRequest(ImageOperation Operation, PixelRect? Region = null, int? Width = null, int? Height = null, float? Amount = null);

namespace Winshott.Domain.Models;

public readonly record struct PixelRect(int X, int Y, int Width, int Height);

/// <summary>Bounds are virtual-desktop logical coordinates; pixel dimensions describe the native capture surface.</summary>
public sealed record DisplayInfo(string Id, string Name, PixelRect Bounds, double ScaleFactor, int PixelWidth = 0, int PixelHeight = 0, bool IsPrimary = false);

public sealed record WindowInfo(string Id, string Title, PixelRect Bounds);

public sealed record CapturedImage(byte[] Data, int Width, int Height, string MediaType = "image/png");

public sealed record ScreenCapturePermission(bool IsGranted, bool CanPrompt, string Message);

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

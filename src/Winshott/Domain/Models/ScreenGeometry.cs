namespace Winshott.Domain.Models;

public static class ScreenGeometry
{
    public static PixelRect LogicalToPixel(PixelRect logicalRect, DisplayInfo display)
    {
        var x = (int)Math.Round((logicalRect.X - display.Bounds.X) * display.ScaleFactor, MidpointRounding.AwayFromZero);
        var y = (int)Math.Round((logicalRect.Y - display.Bounds.Y) * display.ScaleFactor, MidpointRounding.AwayFromZero);
        var right = (int)Math.Round((logicalRect.X + logicalRect.Width - display.Bounds.X) * display.ScaleFactor, MidpointRounding.AwayFromZero);
        var bottom = (int)Math.Round((logicalRect.Y + logicalRect.Height - display.Bounds.Y) * display.ScaleFactor, MidpointRounding.AwayFromZero);
        return new PixelRect(x, y, right - x, bottom - y);
    }

    public static PixelRect ClampToImage(PixelRect rect, int imageWidth, int imageHeight)
    {
        var left = Math.Clamp(rect.X, 0, imageWidth);
        var top = Math.Clamp(rect.Y, 0, imageHeight);
        var right = (int)Math.Clamp((long)rect.X + rect.Width, 0, imageWidth);
        var bottom = (int)Math.Clamp((long)rect.Y + rect.Height, 0, imageHeight);
        return new PixelRect(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
    }
}

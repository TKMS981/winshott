using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Winshott.Application.Interfaces;
using Winshott.Domain.Models;
using Winshott.Infrastructure.Imaging;

namespace Winshott.Infrastructure.Platform;

internal static class MacCaptureNative
{
    private const string Library = "WinshottCapture";
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void JsonCallback(IntPtr context, IntPtr json, IntPtr error);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] internal delegate void ImageCallback(IntPtr context, IntPtr bytes, nint length, int width, int height, IntPtr error);
    private static readonly JsonCallback DisplaysCallback = CompleteJson;
    private static readonly ImageCallback ScreenshotCallback = CompleteImage;
    [DllImport(Library, EntryPoint = "ws_screen_recording_allowed", CallingConvention = CallingConvention.Cdecl)] internal static extern int PermissionAllowed();
    [DllImport(Library, EntryPoint = "ws_request_screen_recording", CallingConvention = CallingConvention.Cdecl)] internal static extern int RequestPermission();
    [DllImport(Library, EntryPoint = "ws_open_screen_recording_settings", CallingConvention = CallingConvention.Cdecl)] internal static extern int OpenSettings();
    [DllImport(Library, EntryPoint = "ws_get_displays_async", CallingConvention = CallingConvention.Cdecl)] internal static extern void GetDisplays(IntPtr context, JsonCallback callback);
    [DllImport(Library, EntryPoint = "ws_capture_display_async", CallingConvention = CallingConvention.Cdecl)] internal static extern void CaptureDisplay([MarshalAs(UnmanagedType.LPUTF8Str)] string id, IntPtr context, ImageCallback callback);
    [DllImport(Library, EntryPoint = "ws_copy_png_to_clipboard", CallingConvention = CallingConvention.Cdecl)] internal static extern int CopyPng(byte[] bytes, int length);

    [ModuleInitializer]
    internal static void RegisterResolver()
    {
        if (OperatingSystem.IsMacOS())
            NativeLibrary.SetDllImportResolver(typeof(MacCaptureNative).Assembly, (name, _, _) => name == Library ? NativeLibrary.Load(Path.Combine(AppContext.BaseDirectory, "libWinshottCapture.dylib")) : IntPtr.Zero);
    }

    internal static Task<string> GetDisplaysAsync(CancellationToken token) => InvokeJsonAsync(GetDisplays, token);

    private static Task<string> InvokeJsonAsync(Action<IntPtr, JsonCallback> call, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handle = GCHandle.Alloc(tcs);
        try { call(GCHandle.ToIntPtr(handle), DisplaysCallback); }
        catch { handle.Free(); throw; }
        return tcs.Task.WaitAsync(token);
    }

    internal static Task<CapturedImage> CaptureAsync(string id, CancellationToken token)
    {
        var tcs = new TaskCompletionSource<CapturedImage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var handle = GCHandle.Alloc(tcs);
        try { CaptureDisplay(id, GCHandle.ToIntPtr(handle), ScreenshotCallback); }
        catch { handle.Free(); throw; }
        return tcs.Task.WaitAsync(token);
    }

    private static void CompleteJson(IntPtr context, IntPtr json, IntPtr error)
    {
        try
        {
            var completion = (TaskCompletionSource<string>)GCHandle.FromIntPtr(context).Target!;
            if (error != IntPtr.Zero) completion.TrySetException(new InvalidOperationException(Marshal.PtrToStringUTF8(error)));
            else completion.TrySetResult(Marshal.PtrToStringUTF8(json) ?? "[]");
        }
        finally { GCHandle.FromIntPtr(context).Free(); }
    }

    private static void CompleteImage(IntPtr context, IntPtr bytes, nint length, int width, int height, IntPtr error)
    {
        try
        {
            var completion = (TaskCompletionSource<CapturedImage>)GCHandle.FromIntPtr(context).Target!;
            if (error != IntPtr.Zero) completion.TrySetException(new InvalidOperationException(Marshal.PtrToStringUTF8(error)));
            else if (bytes == IntPtr.Zero || length <= 0 || length > int.MaxValue) completion.TrySetException(new InvalidDataException("ScreenCaptureKit returned an empty image."));
            else
            {
                var data = new byte[(int)length];
                Marshal.Copy(bytes, data, 0, data.Length);
                completion.TrySetResult(new CapturedImage(data, width, height));
            }
        }
        finally { GCHandle.FromIntPtr(context).Free(); }
    }
}

public sealed class MacScreenCaptureService : IScreenCaptureService, IScreenCapturePermissionService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    public async Task<IReadOnlyList<DisplayInfo>> GetDisplaysAsync(CancellationToken cancellationToken = default)
    {
        var json = await MacCaptureNative.GetDisplaysAsync(cancellationToken);
        var records = JsonSerializer.Deserialize<List<NativeDisplay>>(json, JsonOptions) ?? [];
        return records.Select(d => new DisplayInfo(d.Id, d.Name, new PixelRect(d.X, d.Y, d.Width, d.Height), d.Scale, d.PixelWidth, d.PixelHeight, d.IsPrimary)).ToArray();
    }
    public Task<IReadOnlyList<WindowInfo>> GetWindowsAsync(CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException("Window enumeration and window capture are planned for a later phase.");
    public async Task<CapturedImage> CaptureFullScreenAsync(string? displayId = null, CancellationToken cancellationToken = default)
    {
        var displays = await GetDisplaysAsync(cancellationToken);
        var display = displayId is null ? displays.FirstOrDefault(d => d.IsPrimary) ?? displays.FirstOrDefault() : displays.FirstOrDefault(d => d.Id == displayId);
        if (display is null) throw new InvalidOperationException("No available display was found.");
        return await MacCaptureNative.CaptureAsync(display.Id, cancellationToken);
    }
    public async Task<CapturedImage> CaptureRegionAsync(PixelRect region, CancellationToken cancellationToken = default)
    {
        if (region.Width <= 0 || region.Height <= 0) throw new ArgumentOutOfRangeException(nameof(region), "Selection dimensions must be positive.");
        var displays = await GetDisplaysAsync(cancellationToken);
        var display = displays.FirstOrDefault(d => Intersects(region, d.Bounds)) ?? throw new ArgumentException("Selection does not intersect a display.", nameof(region));
        var source = await CaptureFullScreenAsync(display.Id, cancellationToken);
        var localX = (int)Math.Round((region.X - display.Bounds.X) * display.ScaleFactor);
        var localY = (int)Math.Round((region.Y - display.Bounds.Y) * display.ScaleFactor);
        var width = (int)Math.Round(region.Width * display.ScaleFactor);
        var height = (int)Math.Round(region.Height * display.ScaleFactor);
        return await new SkiaImageProcessingService().ProcessAsync(source, new ImageProcessingRequest(ImageOperation.Crop, new PixelRect(localX, localY, width, height)), cancellationToken);
    }
    public Task<CapturedImage> CaptureWindowAsync(string windowId, CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException("Window capture is not implemented yet.");
    public Task<ScreenCapturePermission> GetPermissionAsync(CancellationToken cancellationToken = default) => Task.FromResult(Permission(MacCaptureNative.PermissionAllowed() == 1));
    public Task<ScreenCapturePermission> RequestPermissionAsync(CancellationToken cancellationToken = default) => Task.FromResult(Permission(MacCaptureNative.RequestPermission() == 1));
    public Task OpenPermissionSettingsAsync(CancellationToken cancellationToken = default)
    {
        if (MacCaptureNative.OpenSettings() != 1) throw new InvalidOperationException("Could not open Screen Recording settings.");
        return Task.CompletedTask;
    }
    private static ScreenCapturePermission Permission(bool granted) => new(granted, !granted, granted ? "Screen Recording permission is granted." : "Screen Recording permission is needed. macOS may require restarting Winshott after changing it in System Settings.");
    private static bool Intersects(PixelRect a, PixelRect b) => a.X < b.X + b.Width && a.X + a.Width > b.X && a.Y < b.Y + b.Height && a.Y + a.Height > b.Y;
    private sealed record NativeDisplay(string Id, string Name, int X, int Y, int Width, int Height, double Scale, int PixelWidth, int PixelHeight, bool IsPrimary);
}

public sealed class MacClipboardService : IClipboardService
{
    public Task CopyImageAsync(CapturedImage image, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsMacOS()) throw new PlatformNotSupportedException();
        if (MacCaptureNative.CopyPng(image.Data, image.Data.Length) != 1) throw new InvalidOperationException("macOS could not copy the image to the clipboard.");
        return Task.CompletedTask;
    }
    public Task CopyTextAsync(string text, CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException("Text clipboard support is not implemented yet.");
    public Task<CapturedImage?> GetImageAsync(CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException("Clipboard reads are not implemented yet.");
    public Task<string?> GetTextAsync(CancellationToken cancellationToken = default) => throw new PlatformNotSupportedException("Clipboard reads are not implemented yet.");
}

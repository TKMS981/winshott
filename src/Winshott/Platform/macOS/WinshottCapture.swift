import AppKit
import ApplicationServices
import CoreGraphics
import Foundation
import ImageIO
import ScreenCaptureKit
import UniformTypeIdentifiers

public typealias JsonCallback = @convention(c) (UnsafeMutableRawPointer?, UnsafePointer<CChar>?, UnsafePointer<CChar>?) -> Void
public typealias ImageCallback = @convention(c) (UnsafeMutableRawPointer?, UnsafePointer<UInt8>?, Int, Int32, Int32, UnsafePointer<CChar>?) -> Void

private func report(_ error: Error, callback: JsonCallback, context: UnsafeMutableRawPointer?) {
    error.localizedDescription.withCString { callback(context, nil, $0) }
}

private func displayName(_ id: CGDirectDisplayID) -> String {
    NSScreen.screens.first { ($0.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")] as? NSNumber)?.uint32Value == id }?.localizedName ?? "Display \(id)"
}

@_cdecl("ws_screen_recording_allowed")
public func wsScreenRecordingAllowed() -> Int32 {
    CGPreflightScreenCaptureAccess() ? 1 : 0
}

@_cdecl("ws_request_screen_recording")
public func wsRequestScreenRecording() -> Int32 {
    CGRequestScreenCaptureAccess() ? 1 : 0
}

@_cdecl("ws_open_screen_recording_settings")
public func wsOpenScreenRecordingSettings() -> Int32 {
    guard let url = URL(string: "x-apple.systempreferences:com.apple.preference.security?Privacy_ScreenCapture") else { return 0 }
    return NSWorkspace.shared.open(url) ? 1 : 0
}

@_cdecl("ws_get_displays_async")
public func wsGetDisplaysAsync(_ context: UnsafeMutableRawPointer?, _ callback: JsonCallback?) {
    guard let callback else { return }
    Task {
        do {
            let content = try await SCShareableContent.excludingDesktopWindows(false, onScreenWindowsOnly: true)
            let list = content.displays.map { display -> [String: Any] in
                let bounds = display.frame
                let id = CGDirectDisplayID(display.displayID)
                let pixelsWide = Int(CGDisplayPixelsWide(id))
                let pixelsHigh = Int(CGDisplayPixelsHigh(id))
                return ["id": String(display.displayID), "name": displayName(id), "x": Int(bounds.origin.x), "y": Int(bounds.origin.y),
                        "width": Int(bounds.width), "height": Int(bounds.height), "scale": bounds.width > 0 ? Double(pixelsWide) / bounds.width : 1,
                        "pixelWidth": pixelsWide, "pixelHeight": pixelsHigh, "isPrimary": CGDisplayIsMain(id) != 0]
            }
            let data = try JSONSerialization.data(withJSONObject: list)
            let json = String(data: data, encoding: .utf8) ?? "[]"
            json.withCString { callback(context, $0, nil) }
        } catch { report(error, callback: callback, context: context) }
    }
}

@_cdecl("ws_capture_display_async")
public func wsCaptureDisplayAsync(_ displayId: UnsafePointer<CChar>?, _ context: UnsafeMutableRawPointer?, _ callback: ImageCallback?) {
    guard let displayId, let callback else { return }
    let requestedId = String(cString: displayId)
    Task {
        do {
            let content = try await SCShareableContent.excludingDesktopWindows(false, onScreenWindowsOnly: true)
            guard let display = content.displays.first(where: { String($0.displayID) == requestedId }) else {
                throw NSError(domain: "WinshottCapture", code: 1, userInfo: [NSLocalizedDescriptionKey: "The selected display is no longer available."])
            }
            let id = CGDirectDisplayID(display.displayID)
            let config = SCStreamConfiguration()
            config.width = Int(CGDisplayPixelsWide(id))
            config.height = Int(CGDisplayPixelsHigh(id))
            config.showsCursor = false
            let filter = SCContentFilter(display: display, excludingWindows: [])
            let image = try await SCScreenshotManager.captureImage(contentFilter: filter, configuration: config)
            let output = NSMutableData()
            guard let destination = CGImageDestinationCreateWithData(output, UTType.png.identifier as CFString, 1, nil) else {
                throw NSError(domain: "WinshottCapture", code: 2, userInfo: [NSLocalizedDescriptionKey: "Could not create PNG encoder."])
            }
            CGImageDestinationAddImage(destination, image, nil)
            guard CGImageDestinationFinalize(destination) else {
                throw NSError(domain: "WinshottCapture", code: 3, userInfo: [NSLocalizedDescriptionKey: "Could not encode display image."])
            }
            let data = output as Data
            data.withUnsafeBytes { bytes in callback(context, bytes.bindMemory(to: UInt8.self).baseAddress, data.count, Int32(image.width), Int32(image.height), nil) }
        } catch {
            error.localizedDescription.withCString { callback(context, nil, 0, 0, 0, $0) }
        }
    }
}

@_cdecl("ws_copy_png_to_clipboard")
public func wsCopyPngToClipboard(_ bytes: UnsafePointer<UInt8>?, _ length: Int32) -> Int32 {
    guard let bytes, length > 0 else { return 0 }
    let data = Data(bytes: bytes, count: Int(length))
    guard let image = NSImage(data: data) else { return 0 }
    let pasteboard = NSPasteboard.general
    pasteboard.clearContents()
    return pasteboard.writeObjects([image]) ? 1 : 0
}

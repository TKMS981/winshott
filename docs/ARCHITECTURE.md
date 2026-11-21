# Architecture

調査日: 2026-11-22。Phase 1実装結果を追記。

## 現状と前提

- 作業ディレクトリに既存ソースはなく、Gitリポジトリも未初期化。
- 開発ホスト: macOS 27.2.1、Xcode 27.2、Command Line Tools の選択先は `/Applications/Xcode.app/Contents/Developer`。
- 初回調査時に `dotnet` は未導入。公式スクリプトから .NET SDK 10.0.404 arm64をリポジトリ内 `.dotnet/` に導入した。環境変数/シェル設定は永続変更せず、helper script経由でローカルSDK・CLI home・NuGet cacheを利用する。
- 製品の基準ターゲットはユーザー指定どおり .NET 10 LTS。調査時点の最新パッチは10.0.15。.NET 11 STSも公開済みだが、LTS要件と互換性確認を優先して.NET 10を採用する。SDKとランタイムのパッチは更新時に再確認する。

## 構成

```text
src/
  Winshott.Domain/           # 座標、画像、撮影履歴などOS非依存のモデル
  Winshott.Application/      # ユースケース、サービス契約、編集/結合ロジック
  Winshott.Infrastructure/   # SkiaSharp、ファイル、JSON設定、ログ
  Winshott.Platform.Abstractions/
  Winshott.Platform.MacOS/   # ScreenCaptureKit、Vision、macOSクリップボード等
  Winshott.Platform.Windows/ # Windows.Graphics.Capture、Windows OCR等
  Winshott.Presentation/     # Avalonia UI、MVVM、テーマ、オーバーレイ
tests/
  Winshott.Domain.Tests/
  Winshott.Application.Tests/
docs/
```

実際のソリューション作成時は、C#のnamespace/プロジェクト名に合わせて `Winshott.Platform.MacOS` のように表記する。UIは単一のAvaloniaデスクトップアプリにし、OS固有サービスは起動時にOS別DI登録する。Domain/ApplicationからOS APIを参照しない。

### 責務の境界

- `IScreenCaptureService`: ディスプレイ・ウィンドウ列挙、全画面/範囲/ウィンドウ撮影。結果は物理ピクセル画像と、ディスプレイID・論理座標から物理ピクセルへの変換情報を持つ。
- `IClipboardService`: 画像とテキストを読み書き。Avaloniaの共通クリップボードを第一候補とし、OS固有形式・遅延レンダリングが必要な場合だけアダプターに閉じ込める。
- `IOcrService` / `IQrCodeService`: OCR文字列と矩形、QR/バーコードの種類・位置・状態を返す。QR結果のURLは表示とコピーだけを行い、自動実行しない。
- `IImageProcessingService`: SkiaSharpによるCrop、Resize、Rotate、Blur、Pixelate、Compose、Stitch。編集は元画像を保持した非破壊コマンド列とUndo/Redoで表現する。
- `IFileService`: PNG/JPEG保存・読み込み・エクスポート、保存先解決。JSON設定はバージョン付きスキーマで保存する。
- `IHotkeyService`: 登録/解除/競合結果。macOSとWindowsのキー表記差は共通ショートカットモデルに変換する。
- `IScrollCaptureController`: OS別の入力/スクロール制御と共通Stitch Engineを分離する。自動スクロールの可否、フォーカス喪失、ユーザー手動操作を明示的な状態として扱う。

画像は大きなメモリ連続割り当てを避け、最大寸法・メモリ予算を設定で制限する。Stitch Engineは複数の帯状候補から重複量を評価し、低信頼度時に手動補正を要求する。座標系は表示論理座標とキャプチャ物理ピクセルを混同しない。

## 技術選定と互換性

| 領域 | 方針 | 互換性・注意 |
|---|---|---|
| .NET | .NET 10 LTS / SDK 10.0.404 | macOS arm64でAvaloniaとテストプロジェクトのビルドを確認。 |
| UI | Avalonia 12.1.6 + CommunityToolkit.Mvvm 8.4.2 | Avalonia package supports net10.0; CommunityToolkit.Mvvm provides ObservableObject/RelayCommand source generators and is MIT licensed. macOS 27.2.1 is beyond the currently documented OS list; app bundle launch was tested successfully on this host. |
| 画像 | SkiaSharp 4.157.0（安定版） | NuGetに.NET 6+資産、Windows/macOSを含む対応とMITライセンスを確認。ネイティブassetsの配布をRIDごとに検証する。 |
| macOS撮影 | ScreenCaptureKitをC ABIの小さなObjective-C/Objective-C++ブリッジから呼ぶ | Appleの推奨API。画面収録のユーザー許可が必要。AvaloniaはmacOS API全体の.NETバインディングを提供しないため、ブリッジのコード量を限定する。 |
| Windows撮影 | Windows.Graphics.CaptureをWindows専用プロジェクトから呼ぶ | ディスプレイとウィンドウのフレーム取得に対応。ユーザー選択UIとシステムのキャプチャ表示枠などOS仕様に従う。既存アプリ全体の無表示キャプチャが可能かは対象OSで検証する。 |
| macOS OCR | Vision `VNRecognizeTextRequest` をネイティブブリッジで利用 | テキスト領域と認識文字列を返す。日本語/英語の利用可能言語はOS実機上で取得し、テストする。 |
| Windows OCR | `Windows.Media.Ocr` を候補にする | MicrosoftのAPI文書ではデスクトップ利用にMSIXのパッケージIDが必要。スタンドアロン実行形式との要件衝突があるため、MSIXを標準配布とし、自己完結型EXEでOCRを提供する方法はPhase 0の未解決事項として実機試作で決定する。 |
| QR/バーコード | ZXing.Net 0.16.11 | NuGetの現行安定版候補、Apache-2.0。画像変換部分の追加依存を避け、SkiaSharpの画素バッファをReaderに渡す薄いアダプターを実装する。 |
| 設定/ログ | System.Text.Json、Microsoft.Extensions.Logging 10.x | .NET共有フレームワークと整合する10.x系列を使い、設定の移行とログの秘匿項目除外を実装する。追加パッケージは必要性が出た場合に限定する。 |
| テスト | xUnit.net v3 4.0.3 + Microsoft Testing Platform | xUnit v3 supports .NET 8+. `global.json` selects MTP for the .NET 10 `dotnet test` integration. Six tests pass. |

NuGetのバージョンはプロジェクト生成時に再確認し、中央パッケージ管理で固定する。依存パッケージとネイティブ同梱物のライセンス一覧をリリース前に作成する。

## Phase 1実装状態

- 単一のAvalonia desktop projectに Domain/Application/Infrastructure/Presentation の責務別フォルダーを配置し、test projectを分離した。初期段階では参照方向を単純に保つため、フォルダー境界とDIを先に設け、project分割はネイティブ実装導入時に行う。
- MainWindow、Capture/OCR/QR/History/Settings画面、ViewModelを実装。アクション未実装箇所はUIに明示した。
- JSON設定はOS別のApplication Support/AppDataに保存。読み込み不良時はログを残して既定値に復帰し、壊れたファイルは自動上書きしない。
- Microsoft.Extensions.Loggingのconsole + ローカルファイルproviderを登録。画像・OCR・クリップボード内容はログに含めない。
- 7個のPlatform interfaceをDI登録。現段階のproviderは未実装操作ごとに`PlatformNotSupportedException`を返す。
- macOS 27.2.1 arm64でRelease self-contained publishとFinder起動可能な`.app`を作成して、実際にメインウィンドウを表示確認した。非対話シェルからraw apphostを実行するとAvalonia Nativeのdisplay render timerが`-6661`で失敗したが、`.app`バンドル経由の起動は成功した。
- 同じ共通UIプロジェクトをmacOS上からWindows x64 self-contained publishできることを確認した。Windows上での起動・画面描画・ネイティブ撮影の動作確認は未実施。

## OS固有の技術課題

1. **画面収録の権限と選択UI**: macOSはScreen Recording許可と、権限が拒否/未設定の場合の案内が必要。Windows Graphics CaptureはOS選択UIやキャプチャ中表示がある。完全に同一の選択フローにせず、共通の撮影結果契約だけを共有する。
2. **Retina/DPIと複数ディスプレイ**: 各ディスプレイの論理原点、物理サイズ、倍率が異なる。負座標を含む仮想デスクトップ座標を保持し、選択オーバーレイ/拡大鏡の座標変換を一箇所に集約する。
3. **macOSネイティブ連携**: ScreenCaptureKit/Visionの非同期APIとObjective-Cオブジェクト寿命をC ABIで扱う。Swiftを主要ロジックにせず、小さなブリッジと明示的なバッファ所有権に限定する。
4. **Windows API連携**: WinRTフレームプールのDispatcher/スレッド制約、D3DデバイスとSkiaSharp間のCPU/GPU転送、ウィンドウ終了/サイズ変更/デバイスロストを処理する。
5. **Windows OCRと配布形式**: Windows.Media.OcrはパッケージIDを前提にする。MSIXを主配布にするか、EXE向けに別OCRエンジンを用意するかを、ライセンス・配布サイズ・言語モデル・速度を比較して決める。
6. **スクロール撮影**: 一般OS APIに汎用の安全な自動スクロール機能はない。アプリへ入力を送る方法はフォーカスや権限に左右される。最初は手動スクロール撮影を確実にし、自動スクロールは対象アプリ別の制限と停止条件を明確化する。
7. **グローバルホットキー**: macOSでは権限/予約キー、Windowsでは登録衝突や修飾キー差がある。起動時に登録可否をUIで通知し、設定変更時に再登録する。
8. **ウィンドウ撮影**: 最小化/保護コンテンツ/権限などで取得できないウィンドウがある。失敗理由をサービス結果に含め、画面全体からの選択を代替にする。

## セキュリティとプライバシー

画面画像とOCR結果はローカルに限定する。ログに画像内容、OCR本文、クリップボード内容を記録しない。画像の外部送信やQR URLの起動は行わない。権限は撮影/必要なホットキーに限定し、OS設定画面への案内を出す。

## 参照資料

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- [Avalonia supported platforms](https://docs.avaloniaui.net/docs/supported-platforms)
- [Avalonia macOS guide](https://docs.avaloniaui.net/docs/platform-specific-guides/macos)
- [ScreenCaptureKit](https://developer.apple.com/documentation/ScreenCaptureKit)
- [Windows.Graphics.Capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)
- [Apple Vision text recognition](https://developer.apple.com/documentation/vision/vnrecognizetextrequest)
- [Windows.Media.Ocr](https://learn.microsoft.com/en-us/uwp/api/windows.media.ocr)
- [Avalonia NuGet](https://www.nuget.org/packages/Avalonia/)
- [SkiaSharp NuGet](https://www.nuget.org/packages/SkiaSharp/)
- [ZXing.Net NuGet](https://www.nuget.org/packages/ZXing.Net/)
- [xUnit v3 NuGet](https://www.nuget.org/packages/xunit.v3/)

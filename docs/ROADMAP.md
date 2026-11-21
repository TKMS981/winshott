# Roadmap

調査日: 2026-11-22。実機確認できた段階だけを完了扱いにする。

## Phase 0 — 環境調査と設計（このタスク）

- [x] 作業ディレクトリと既存ファイルを確認
- [x] macOS、.NET、Xcode Command Line Toolsを確認
- [x] 主要技術の現行互換性・ライセンス・OS制約を調査
- [x] アーキテクチャ、機能一覧、ロードマップを文書化
- [x] .NET 10 SDKを導入し、Avaloniaの最小アプリをビルド・起動（Phase 1）

## Phase 1 — アプリケーション基盤（完了）

1. .NET 10 SDK導入後に、Avalonia 12の最小デスクトップアプリを作成してmacOSで起動。
2. Domain/Application/Platform Abstractions/Presentationを作り、DI、MVVM、JSON設定、ログを構成。
3. サービス契約と撮影画像モデル、論理/物理座標モデルを実装。
4. メイン画面、テーマ切替、エラー通知、設定読み書きを確認。

結果: macOS 27.2.1 arm64でself-contained `.app`からウィンドウ表示を確認。Windows x64 self-contained publishもmacOS上で成功した。`dotnet build`は0警告/0エラー、xUnit 6件成功。Windows上の起動確認と撮影/OCR/QRの各OS実装はPhase 1の対象外で、UI上も未実装と表示。

## Phase 2 — 基本撮影（部分実装・実機確認待ち）

1. [x] macOS ScreenCaptureKitのディスプレイ列挙、全ディスプレイ情報、単一ディスプレイ全画面撮影、権限案内UI。
2. [~] 静止フレーム上のドラッグ範囲選択、Escape/Enter、論理/物理座標変換。全画面オーバーレイ、枠移動/リサイズ、複数ディスプレイ横断は残作業。
3. [x] 撮影結果プレビュー、macOS画像クリップボード書込み、Avaloniaローカル保存picker、PNG/JPEG保存と共通Skia crop。
4. [ ] macOSウィンドウ撮影、複数ディスプレイ横断選択、Retina実画面撮影の実機確認。
5. [~] ScreenCaptureKitの例外表示とキャンセルtoken。大画像のメモリ上限、失敗時の実機検証は残作業。
6. [ ] 新しいself-contained `.app`の起動、Screen Recording許可状態、画面キャプチャの実画面確認。LaunchServicesはテストbundleを`kLSNoExecutableErr`として拒否した。

## Phase 3 — スクロール撮影

1. 手動スクロールとフレーム取り込みのセッション状態を実装。
2. 共通Stitch Engineで複数行特徴/画素差による重複検出、信頼度を算出。
3. 結果プレビュー、手動結合位置補正、上限/一時停止/再開/キャンセル。
4. 安定後にOS別自動スクロールを検討し、対象アプリと安全な停止条件を決める。

## Phase 4 — OCR・QR

1. macOS Visionで日本語/英語の文字列と座標を取得。
2. ZXing.Netで複数QR/対応バーコードの種類・位置・認識状態を取得。
3. 範囲処理、結果一覧、コピー、URLの非自動実行を追加。
4. Windows OCRのMSIX/EXE配布方針を確定し、Windows実装を追加。

## Phase 5 — 編集機能

1. 非破壊コマンドモデル、Undo/Redo。
2. 矢印、四角、円、テキスト、フリーハンド。
3. モザイク、ぼかし、トリミング、リサイズ。
4. エクスポート画像に編集レイヤーを合成。

## Phase 6 — 追加機能

- カラーピッカー、常に手前のPinウィンドウ、遅延撮影、履歴。
- 保存先、形式、ホットキー、最大画像サイズなどの設定。

## Phase 7 — Windows 11

1. Windows.Graphics.Captureとディスプレイ/ウィンドウ一覧。
2. Windowsクリップボード、PNG/JPEG、Windows OCRとホットキー。
3. DPI、マルチモニター、ウィンドウ終了/サイズ変更、権限/OS表示を確認。
4. スクロール撮影の手動/自動経路を検証。
5. MSIXを標準成果物とし、自己完結型EXEも必要な場合はAPI制約に対応する。

## Phase 8 — 品質と配布

- macOS/Windowsで撮影、コピー、保存、OCR、QR、編集、異常系を確認。
- 大画像のピークメモリ、速度、長時間使用、ログ、権限エラーを確認。
- 依存関係/ライセンス一覧、インストーラー、署名、更新手順を整備。

## 次の実装単位

次の実装単位はPhase 2のmacOSディスプレイ列挙と全画面撮影。Windows OCRの配布条件はWindows実機試作で判断する。

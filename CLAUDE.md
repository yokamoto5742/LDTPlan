# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## アプリ概要

生活習慣病療養計画書作成Windowsデスクトップアプリ（C# / WinForms / .NET 10）。患者情報をSQLiteで管理し、Excelテンプレート（xlsm）にデータを流し込んでバーコード付き文書を生成する。

**エントリーポイント**: `Program.cs` → `Application.Run(new MainForm())`

`src/` はPython/Flet版（旧実装）の参照用コピー。機能追加・修正はすべてC#側に行う。

主要パッケージ: EF Core Sqlite（ORM）・ClosedXML（xlsm読み書き、VBAマクロ保持）・ZXing.Net（Code128バーコード生成）

## コマンド

```bash
# ビルド
dotnet build LDTPlan.csproj

# アプリ実行
dotnet run --project LDTPlan.csproj

# リリースビルド（配布用）
dotnet publish LDTPlan.csproj -c Release
```

テストプロジェクトは未整備。動作検証はscratchpadのファイルベースアプリ（`dotnet run <file>.cs`）でサービス層を直接呼び出して行った実績あり（EF Core利用時は `#:property PublishAot=false` が必要）。

## アーキテクチャ

三層構造：

```
UI層     Forms/           WinFormsフォーム・イベントハンドラ
サービス層 Services/        ビジネスロジック（Excel生成・患者操作・CSV入出力・ファイル監視）
データ層  Models/ Data/    EF Coreエンティティ・DbContext・初期データ投入
```

### UI層 (`Forms/`)

- `MainForm.cs`: ホーム画面。患者ID入力で pat.csv から基本情報を自動読込し、計画書一覧（DataGridView）を表示。行クリックで編集画面を開く。前回計画コピーもここ。
- `PlanEditForm.cs`: 新規作成・編集・テンプレート編集の3モードを `PlanFormMode` で共用。主病名変更→シート名絞り込み、シート名変更→テンプレート自動適用、喫煙チェックボックスの排他制御。
- `SettingsDialog.cs`: バージョン表示・CSV出力/取込。
- `UiFactory.cs`: ラベル・コンボボックス・行パネル等の生成ヘルパー。

### サービス層 (`Services/`)

- `TreatmentPlanGenerator.cs`: `CreatePlanFile()` がClosedXMLでテンプレート読込 → 共通情報シートへセル入力 → Code128バーコード画像を初回用・継続用シートに配置 → xlsm保存。`GeneratePlan()` は生成後にExcelで開く。初回用/継続用の切替は作成回数（`CreationCount`）が1かどうかで判定し、B13セルに作成回数を出力する。
- `PatientCsvService.cs`: pat.csv（Shift_JIS・ヘッダなし・先頭3行）から患者データ読込。
- `PatientService.cs`: 主病名・シート名マスタ読込、計画書履歴取得。
- `DataExportService.cs`: 患者情報のCSV出力/取込（Python版のエクスポート形式と互換）。
- `FileMonitorService.cs`: FileSystemWatcherで pat.csv を監視。削除でアプリ終了。

### データ層 (`Models/` + `Data/`)

- `Data/AppDbContext.cs`: EF Core DbContext。操作ごとに `using var db = new AppDbContext()` で生成する。
- `Data/DbInitializer.cs`: 起動時に `EnsureCreated()` でテーブル作成、マスタ（主病名3件・シート名9件・テンプレート9件）を投入。
- モデル: `PatientInfo`（47フィールド）・`MainDisease`・`SheetName`・`PlanTemplate`。テーブル名・列名は `[Table]`/`[Column]` 属性でPython版SQLite DBと互換。

### 設定 (`config.ini`)

プロジェクト直下にあり、ビルド時に出力先へコピーされる（実行時はexeと同じディレクトリから読む）。`Utils/AppConfig.cs` で読み込む。主要セクション: `[Database]`・`[Paths]`・`[FilePaths]`・`[Barcode]`・`[settings]`。

### UI文字列 (`Constants.cs`)

画面表示文字列・ドロップダウン選択肢は `Constants.cs` の定数を参照し、コード内にマジック文字列を書かない。

## DBスキーマ変更手順

1. `Models/PatientInfo.cs` に `[Column("列名")]` 付きプロパティを追加
2. `EnsureCreated()` は既存DBを変更しないため、既存の `ldtp_app.db` には手動で `ALTER TABLE patient_info ADD COLUMN ...` を適用（新規DBは自動作成される）
3. サービス層（CSV入出力の列定義含む）・UI層を更新

## Excelテンプレート変更手順

1. `C:\Shinseikai\LDTPapp\LDTPform.xlsm` を編集
2. `TreatmentPlanGenerator.PopulateCommonSheet()` のセル指定を更新
3. VBAマクロはClosedXMLが自動保持する

# コードレビュー 2026-10-13

対象: C#版 LDTPlan 全体（コミット `3fc2a9e` 時点、`.cs` 20ファイル・約2,200行）
観点: 可読性・メンテナンス性、KISS原則

## 総評

三層構造が守られており、1ファイルあたりの規模も小さく、全体として読みやすい。`UiFactory` と `Constants` による画面構築の定型化、`DataExportService.Columns` の列定義テーブルは良い設計。

改善の余地が大きいのは次の2点。

1. **患者基本情報の受け渡しが「型付き → 文字列 → 型付き」と往復している**（指摘 1）。ここを直すと `PlanEditForm` から分岐とパース処理がまとめて消える。
2. **エラーの返し方がサービスごとに違う**（指摘 4）。例外・文字列・タプルの3通りが混在している。

動作上の不具合も4件見つかった（指摘 2, 3, 5, 6）。いずれも修正は数行で済む。

優先度の目安: **高** = 不具合または変更コストに直結 / **中** = 単純化の効果が大きい / **低** = 気づいたら直す程度

---

## 優先度: 高

### 1. 患者基本情報を文字列で往復させている

`Forms/MainForm.cs:192-210`, `Forms/PlanEditForm.cs:18-29, 458-502`

現状の流れ:

```
PatientCsvRecord（int / DateTime）
  → MainForm のテキストボックス（文字列）
  → PatientBasicInfo（すべて string）
  → PlanEditForm で ParseIntOrThrow / DateTime.ParseExact して int / DateTime に戻す
```

さらに `PlanEditForm` は `_csvPatients` も受け取り、`CreatePatientInfoFromCsv()` で同じ患者をもう一度検索している。氏名・カナ・性別・生年月日は CSV レコードから、医師・診療科は `PatientBasicInfo` から取っているが、どちらも元は同じ CSV の同じ行である。

この結果、次のコードが必要になっている。

- `PatientBasicInfo` クラス（9プロパティ）
- `ParseIntOrThrow` と、基本情報用のエラーメッセージ
- `ApplyFormToPatientInfo` の `includeBasicInfo` フラグによる分岐
- `CreatePatientInfoFromCsv` 内の患者ID再パースと再検索

**提案**: `MainForm` が選択中の `PatientCsvRecord?` を保持し、それをそのまま `PlanEditForm` に渡す。

```csharp
// MainForm
using var form = new PlanEditForm(mode, _currentPatient, recordId);

// PlanEditForm
private void ApplyBasicInfo(PatientInfo p, PatientCsvRecord csv)
{
    p.PatientId = csv.PatientId;
    p.PatientName = csv.Name;
    p.Kana = csv.Kana;
    p.Gender = csv.Gender;
    p.Birthdate = csv.Birthdate;
    p.DoctorId = csv.DoctorId;
    p.DoctorName = csv.DoctorName;
    p.DepartmentId = csv.DepartmentId;
    p.Department = csv.Department;
}
```

`PatientBasicInfo`・`_csvPatients` 引数・基本情報のパース処理が不要になり、新規作成と編集保存が同じ1メソッドを使える。

副次的に、現状の次の挙動も解消される。pat.csv に存在しない患者IDを手入力して履歴から編集画面を開くと、テキストボックスが空のため「保存」が必ず失敗する（`ParseIntOrThrow("")` が例外）。`_currentPatient == null` を明示的に判定できるようになるので、原因が分かるメッセージを出せる。

### 2. 「新規登録して印刷」で計画書生成に失敗すると、再実行でレコードが重複する

`Forms/PlanEditForm.cs:325-353`

`db.SaveChanges()` の後に `TreatmentPlanGenerator.GeneratePlan()` を呼んでいる。テンプレートが見つからない・出力先に書けないなどで例外になると、レコードは保存済みのままエラー表示だけが出て画面は閉じない。利用者がもう一度ボタンを押すと同じ計画書が2件登録される。

**提案**: 保存が済んだら生成の成否にかかわらず画面を閉じる（生成失敗は「保存はされたが計画書を作成できなかった」と伝える）。編集画面の「印刷」から再生成できるので、利用者の操作も途切れない。

### 3. 保存エラーのメッセージが二重になる・原因と合わない

`Forms/PlanEditForm.cs:372-375, 540-543`

`ParseIntOrThrow` はすでに `MsgInvalidInputFormat` で整形したメッセージを投げるが、`SaveData` の `catch` がもう一度同じ書式で包む。作成回数に数字以外を入れると次のように表示される。

```
入力値が不正です: 入力値が不正です: 作成回数
```

また `catch (Exception)` なので、DBのロックなど入力と無関係なエラーも「入力値が不正です」と表示される。一方 `SaveNewPlan` と `PrintPlan` は `ex.Message` をそのまま出しており、3つのボタンで扱いが揃っていない。

目標体重は `double.Parse` の例外がそのまま出るため、ランタイムの汎用メッセージが表示され、どの項目かが分からない。

**提案**: 3か所とも `ShowError(ex.Message)` に揃え、目標体重も `ParseIntOrThrow` と同じ形の `ParseDoubleOrThrow`（項目名付き）にする。指摘 1 を実施すれば入力値のパースは「作成回数」と「目標体重」の2つだけになる。

### 4. エラーの返し方が3通りある

| 場所 | 返し方 |
|---|---|
| `TreatmentPlanGenerator` | 例外を投げる |
| `DataExportService.ImportFromCsv` | エラー文字列（成功時 `null`） |
| `DataExportService.ExportToCsv` | `(FileName, FilePath, Error)` タプル |
| `PatientCsvService.LoadPatientData` | `(ErrorMessage, Records)` タプル |

呼び出し側はメソッドごとに判定方法を覚える必要がある。加えて、返している値の一部は誰も使っていない。

- `ExportToCsv` の `FilePath` — 唯一の呼び出し元 `SettingsDialog.cs:48` が破棄
- `LoadPatientData` の `ErrorMessage` — 唯一の呼び出し元 `MainForm.cs:128` が破棄。そのため pat.csv の読込に失敗しても、利用者には空の画面が出るだけで理由が分からない

**提案**: サービス層は例外を投げ、UI層の `try/catch` で表示する形に統一する（`PlanEditForm` はすでにこの形）。戻り値は `string ExportToCsv(...)`（ファイル名）、`void ImportFromCsv(...)`、`List<PatientCsvRecord> LoadPatientData()` になり、タプルと `null` 判定が消える。

### 5. DBファイルのパスが作業ディレクトリ基準になっている

`Utils/AppConfig.cs:44-53`, `Data/DbInitializer.cs:12`, `Data/AppDbContext.cs:17`

`config.ini` と `startup.log` は `AppContext.BaseDirectory`（exeのフォルダ）基準だが、`DbPath` は `ldtp_app.db` という相対パスのまま使われるため、プロセスの作業ディレクトリ基準になる。ショートカットの「作業フォルダー」や、電子カルテなど別プロセスからの起動で作業ディレクトリが変わると、`File.Exists` が偽になり、その場所に空のDBが新規作成される。利用者からは「データが消えた」ように見える。

**提案**: `DbPath` を `Path.Combine(AppContext.BaseDirectory, ...)` で解決する（`Path.Combine` は第2引数が絶対パスならそれを優先するので、絶対パス指定の設定も壊れない）。

---

## 優先度: 中

### 6. 患者IDが数値でないとき、基本情報の表示が前の患者のまま残る

`Forms/MainForm.cs:139-144`

`OnPatientIdChanged` は数値に変換できたときだけ `LoadPatientInfo` を呼ぶ。患者IDを消す・数字以外を入れると、履歴は空になるが氏名や医師名は直前の患者のまま表示される。

同じ `int.TryParse(_txtPatientId.Text.Trim(), ...)` が `OnPatientIdChanged`・`RefreshHistory`・`CopyData` の3か所にあるのも読みにくい。

**提案**: 指摘 1 の `_currentPatient` に寄せる。

```csharp
private void OnPatientIdChanged(object? sender, EventArgs e)
{
    _currentPatient = int.TryParse(_txtPatientId.Text.Trim(), out var id)
        ? _csvPatients.FirstOrDefault(r => r.PatientId == id)
        : null;
    ShowPatientInfo(_currentPatient);   // null なら全欄クリア
    RefreshHistory();
}
```

### 7. `CopyData` が全フィールドを手書きで複写している

`Forms/MainForm.cs:231-273`

40行のオブジェクト初期化子で `source` の全列を書き写している。`PatientInfo` に列を追加したとき、ここを更新し忘れてもビルドは通り、コピーした計画書でその列だけ空になる。

**提案**: 追跡なしで読み込み、主キーを 0 に戻して追加する。変更する項目だけが残る。

```csharp
var copy = db.PatientInfos.AsNoTracking()
    .Where(p => p.PatientId == patientId)
    .OrderByDescending(p => p.Id)
    .FirstOrDefault();
if (copy == null)
    return;

copy.Id = 0;
copy.IssueDate = today;
copy.IssueDateAge = ...;
copy.DoctorId = csvRecord.DoctorId;
// 医師名・診療科・作成回数
db.PatientInfos.Add(copy);
```

なお、フィールドの対応表は他に `LoadRecord` / `ApplyGuidanceFields` / `ApplyTemplate` / `SaveTemplate` / `DataExportService.Columns` / `PopulateCommonSheet` にもある。これらは画面コントロールやセル番地との対応であり、共通化するとかえって抽象が増えるので、現状の明示的な列挙のままでよい。CLAUDE.md の「DBスキーマ変更手順」に、更新が必要な箇所としてこの6つを列挙しておくと漏れを防げる。

### 8. 到達しないコード

| 場所 | 内容 |
|---|---|
| `PlanEditForm.cs:98-100` | テンプレートモードの `ApplyTemplate()`。コンストラクタ時点では主病名・シート名が未選択なので、必ず冒頭の `return` で抜ける |
| `PlanEditForm.cs:401-405` | `DeleteData` の `_recordId == null` 判定。削除ボタンは編集モードにしかなく、編集モードは必ず `recordId` 付きで開かれる。`MsgNoDataSelected` も不要になる |
| `DbInitializer.cs:25, 37, 55` | `Seed*` の `if (db.X.Any()) return;`。`Initialize` はDBファイルがないときしか実行されないので、テーブルは必ず空 |
| `PatientService.cs:54` | `.OrderBy(p => p.PatientId)`。直前の `Where` で患者IDを1つに絞っている |

`DbInitializer` はガードを外せば `SaveChanges()` も最後の1回で済む。

### 9. 設定値の読み方が2通りある

`Utils/AppConfig.cs`, `Forms/MainForm.cs:54-63`, `Forms/PlanEditForm.cs:112`, `Services/TreatmentPlanGenerator.cs:162-204`

パス類は `AppConfig.TemplatePath` のような型付きプロパティだが、それ以外は `AppConfig.GetInt("Barcode", "image_width", 200)` のように、セクション名・キー名・既定値を呼び出し側に直書きしている。同じキーが複数箇所にあり、既定値も重複している。

- `"Barcode", "image_width"` / `"image_height"` — `CreateBarcodePng` と `AddBarcodeToSheet` の2か所ずつ
- `"UI", "heading_font_size"` と `* 0.75f` の換算 — `MainForm` と `PlanEditForm`

**提案**: `AppConfig` のプロパティに寄せる（`BarcodeImageWidth`、`HeadingFontSize` など）。キー名と既定値の定義が1か所になる。

あわせて `config.ini` の次のキーはC#側から参照されていない（Python版の名残）。残すと「変えれば効く」と誤解されるので削除を勧める。

- `[UI]` `input_height` / `text_height` / `font_size`
- `[Barcode]` `write_text` / `module_height` / `module_width`

### 10. `PatientCsvService` の列番号がマジックナンバー

`Services/PatientCsvService.cs:38-51`

`f[2]`, `f[9]`, `f[13]`, `f.Count < 15` と数値が並び、pat.csv の列仕様を知らないと読めない。`ParseInt` は変換に失敗すると黙って `0` を返すので、列がずれたときに患者ID 0 のレコードができ、原因に気づきにくい。

**提案**: 列番号に名前を付ける（`const int ColPatientId = 2;` など）か、少なくとも列レイアウトを1行コメントで示す。患者IDが変換できない行は `f.Count < 15` と同様に読み飛ばす。

### 11. 削除に確認がない

`Forms/PlanEditForm.cs:399-415`

「削除」を押すと即座にレコードが消える。「保存」「印刷」の隣にあるので押し間違いが起きやすく、元に戻す手段もない。他のDB操作と違い `try/catch` もない。`MessageBox` による確認を1つ入れることを勧める。

---

## 優先度: 低

### 12. `SetCell` の `switch`

`Services/TreatmentPlanGenerator.cs:121-148`

`object?` で受けて型ごとに分岐し、同じ `cell.Value = x` を6回書いている。ClosedXML の `XLCellValue.FromObject(value)` で1行にできる可能性がある（`null` は空白セルになる）。使用中のバージョンで日付・真偽値の出力が変わらないことを、生成した xlsm で確認してから置き換えること。

### 13. パッケージのバージョンがワイルドカード指定

`LDTPlan.csproj:32-35`

`ClosedXML` が `0.*`、`SQLitePCLRaw` が `3.*` など。復元した日によって入るバージョンが変わるため、同じソースから同じ配布物ができる保証がない。`RestoreLegacyDrawings` は ClosedXML の保存時の挙動に依存しているので、ライブラリ更新の影響を特に受けやすい。バージョンを固定し、更新は意図して行うことを勧める。

### 14. csproj が存在しないファイルを参照している

`LDTPlan.csproj:19-29`

`.claude\agents\japanese-comment-reviewer.md` / `pyright-type-checker.md` / `pytest-test-creator.md` はリポジトリに存在しない。この `<ItemGroup>` はビルドに関係しないので、まるごと削除してよい。

### 15. 定数化の漏れ

CLAUDE.md の「コード内にマジック文字列を書かない」に対して、次が残っている。

- 画面文字列: `"kg"` / `"mmHg"` / `"%"`（`PlanEditForm.cs:150-152`）、`"CSVファイル (*.csv)|*.csv"` とバージョン表示の書式（`SettingsDialog.cs:26, 62`）、`"エラー: "`（`PatientCsvService.cs:59`、指摘 4 を実施すれば消える）
- 日付書式: `"yyyy/MM/dd"` が4か所（`PlanEditForm.cs:45, 493`、`MainForm.cs:154`、`PatientService.cs:60`）、`"yyyy-MM-dd"` が3か所（`AppDbContext.cs:23`、`DataExportService.cs:135, 151`）

日付書式は表示用と保存用の2つを定数にすれば足りる。

### 16. その他の小さな点

- `PlanEditForm.cs:39-40`: フィールド初期化子でDBを読んでいる。`BuildUi()` の冒頭に移すと、DBアクセスの位置がコンストラクタの流れから読み取れる。
- `PlanEditForm.cs:231-232, 274-275`: 「主病名から主病名IDを引き、シート名を絞り込む」2行が重複し、DBにも2回アクセスしている。`PatientService.LoadSheetNames(string mainDiseaseName)` にまとめられる。
- `PlanEditForm.cs:380`: `PrintPlan` だけ `CheckRequiredFields()` を呼ばない。シート名未選択のまま保存・印刷できる。
- `DataExportService.cs:94`: ファイル名の判定は正規表現でなく `StartsWith("patient_info_")` で足りる。
- `TreatmentPlanGenerator.cs:152-156`: `ToString().PadLeft(9, '0')` は `ToString("D9")` と書ける。`DateTime.Now` を2回取得しているので、1回にまとめると日付をまたぐ瞬間のずれもなくなる。
- `FileMonitorService.cs:10`: サービス層が `Form` を受け取っている。`Action onDeleted` を受け取る形にすればUI層への依存が消える。
- `Constants.cs:7`: `AppLastUpdated` が `2026-10-12` だが、CHANGELOG の 1.1.7 は `2026-10-13`。

### 17. ドキュメントと実装のずれ

- `CLAUDE.md`: 「`src/` はPython/Flet版の参照用コピー」とあるが、`src/` は `3fc2a9e` で削除済み。
- `CLAUDE.md` / `README.md`: テンプレート等のパスが `C:\Shinseikai\LDTPapp\...` だが、`config.ini` と csproj の配置先は `C:\Shinseikai\LDTPlan\...`。
- `SettingsDialog.cs:26`: バージョン表示のアプリ名が `LDTPapp`。

---

## 進め方の提案

互いに依存しない順に並べた。各ステップは単独でコミットできる。

1. 指摘 2, 3, 5 — 不具合の修正（各数行）
2. 指摘 1, 6 — `PatientCsvRecord` を直接渡す形にする（`PlanEditForm` が最も短くなる変更）
3. 指摘 4 — サービス層のエラーを例外に統一
4. 指摘 7, 8 — `CopyData` の単純化と到達しないコードの削除
5. 指摘 9 以降 — 手が空いたときに

テストプロジェクトがないため、ステップ 2 と 4 の前後で「新規登録 → 編集保存 → 印刷 → 前回計画コピー → CSV出力/取込」を手動で通して確認すること。

# Visual Studio で発行（publish）する手順

`dotnet publish LDTPlan.csproj -c Release` と同じ処理を Visual Studio の **発行** ボタンで行うための設定手順。

「発行プロファイル」を一度作れば、以後は **発行** ボタン1つで単一ファイル版の `LDTPlan.exe` が生成され、配布フォルダ `C:\Shinseikai\LDTPlan` へコピーされる。

## 設定手順

1. ソリューション エクスプローラーで `LDTPlan` プロジェクトを右クリックし、**発行** を選ぶ
2. ターゲットで **フォルダー** を選び、**次へ**。特定のターゲットも **フォルダー** を選ぶ
3. フォルダーの場所は既定の `bin\Release\net10.0-windows\win-x64\publish\` のまま **完了**
4. 発行画面の **すべての設定を表示** を開き、次のようにして **保存** する

| 項目 | 値 |
|---|---|
| 構成 | Release |
| ターゲット フレームワーク | net10.0-windows |
| 配置モード | 自己完結 |
| ターゲット ランタイム | win-x64 |
| ファイルの公開オプション | 「単一ファイルの作成」「ReadyToRun コンパイルを有効にする」にチェック |

5. **発行** ボタンを押す

設定は `Properties\PublishProfiles\FolderProfile.pubxml` に保存される。

## 注意点

### 発行先を `C:\Shinseikai\LDTPlan` にしない

発行先を直接配布フォルダにして「発行前に既存のファイルをすべて削除する」にチェックが入っていると、`ldtp_app.db` や `LDTPform.xlsm` まで消える。

発行先は `publish\` のままにし、配布フォルダへのコピーは `LDTPlan.csproj` の `CopyToDeployFolder` ターゲットに任せる。このターゲットは発行後に次の3ファイルをコピーする。

- `LDTPlan.exe`
- `e_sqlite3.dll`
- `config.ini`

### コピーされたかは初回に確認する

`CopyToDeployFolder` は Visual Studio の発行でも動く想定だが、VS 経由での動作は未確認。初回の発行後に次を確認する。

- `C:\Shinseikai\LDTPlan\LDTPlan.exe` の更新日時が発行時刻になっている
- サイズが約183MBである（162KB の場合は単一ファイル版ではない）

### 「ビルド」や「リビルド」では単一ファイル版はできない

`PublishSingleFile` が効くのは発行のときだけ。ビルドで `bin\Release\net10.0-windows\win-x64\` 直下に出力される `LDTPlan.exe`（162KB）は、同じフォルダの `LDTPlan.dll` やランタイム DLL 群を読み込むランチャーであり、単体でコピーしても起動しない。

その場合、Windows イベントログ（.NET Runtime 1023）に次のエラーが記録される。

```
Message: The application to execute does not exist: 'C:\Shinseikai\LDTPlan\LDTPlan.dll'.
```

配布時は必ず **発行** を使う。

## 発行に時間がかかる場合

通常は数秒（キャッシュが効いた状態で約4秒）で終わる。1分以上かかる場合に考えられる原因は次のとおり。

- **ReadyToRun の再コンパイル**: `obj` 削除後、SDK 更新後、パッケージ更新後の初回は ClosedXML・OpenXml・EF Core などを全部コンパイルし直すため、1分程度かかっても正常
- **ビルドの競合**: Visual Studio のビルドとコマンドラインの publish を同時に実行すると、ファイルロック待ちで遅くなる
- **ウイルス対策のスキャン**: 約183MB の exe を生成・コピーするたびにスキャンされると、数十秒単位で延びることがある

コマンドラインで次を実行すると、タスクごとの所要時間が末尾に表示される。

```bash
dotnet publish LDTPlan.csproj -c Release -clp:PerformanceSummary
```

`Crossgen2` が長ければ再コンパイル、`Copy` や `GenerateBundle` が長ければロックかスキャンが原因。

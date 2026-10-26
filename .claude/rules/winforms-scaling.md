# winforms-scaling.md

WinForms 画面の文字の大きさとレイアウトの指針。一部の端末だけで文字が大きくなり、画面が切れた不具合の再発を防ぐ。

## 前提：Windows の拡大設定は2種類ある

| 設定 | 場所 | アプリへの影響 |
|---|---|---|
| ディスプレイの拡大縮小（DPI） | 設定 > ディスプレイ > 拡大縮小 | タイトルバーを含む画面全体が拡大する |
| テキストのサイズ | Win11: 設定 > アクセシビリティ > テキストのサイズ / Win10: 設定 > 簡単操作 > ディスプレイ > 文字を大きくする | WinForms の既定フォントだけが拡大する |

- 「DPIは100%なのに文字だけ大きい」「タイトルバーは通常の大きさ」のときは、テキストのサイズを疑う。
- テキストのサイズの倍率は、レジストリ `HKCU\Software\Microsoft\Accessibility` の `TextScaleFactor`（DWORD、100〜225）に入っている。
- 開発機の設定も100%とは限らない。この開発機は120%に設定されている。

## WinForms の仕様

- .NET の WinForms は、`Application.SetDefaultFont` に渡したフォントを `TextScaleFactor` の倍率で拡大し、`Control.DefaultFont` として返す。
- フォントの単位を `GraphicsUnit.Pixel` にしても、`AutoScaleMode` を変えても、この拡大は止まらない。
  - `AutoScaleMode` は、`AutoScaleDimensions` を持たず、コードだけで組み立てたフォームでは働かない。
- `SetDefaultFont` は、最初の呼び出しの結果がキャッシュされる。2回目の呼び出しは `Control.DefaultFont` に反映されない。

## このアプリの方針

- 文字の大きさは、端末の設定に関わらず `config.ini` の `font_size` にそろえる。
  - 実装は `Program.cs` で、`AppConfig.FontSize / SystemTextScale.Factor` を `SetDefaultFont` に渡し、倍率を打ち消している。
- この補正を外したり、`SetDefaultFont` を別の場所で呼んだりしない。

## 画面を作るときの注意

- コントロールの幅・高さは `UiFactory.ScaleToFont`（`Control.DefaultFont.Size` が基準）で決める。ピクセルの直書きはしない。
- フォームの大きさ（`ClientSize`）はピクセル固定で、文字に追従しない。行に項目やボタンを足したら、`config.ini` の画面幅に収まるかを確かめる。
- 別のフォントを作るときは、`Font.FontFamily` と `UiFactory.HeadingFontSize` のように既定フォントを基準にする。サイズを固定値で書かない。

## 確かめ方

- 表示の不具合の報告を受けたら、まずその端末の DPI とテキストのサイズの両方を確認する。
- 画面を変えたら、テキストのサイズを125%以上にした状態でも起動し、通常と同じ見た目になるかを確かめる。
- `Control.DefaultFont.Size` の値は、検証用の小さなプログラムを別プロセスで動かして確かめられる。`SetDefaultFont` がキャッシュされるため、1つのプロセスでは比較できない。

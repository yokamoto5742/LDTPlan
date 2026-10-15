using System.Diagnostics;
using LDTPlan.Services;
using LDTPlan.Utils;
using static LDTPlan.Forms.UiFactory;

namespace LDTPlan.Forms;

/// <summary>バージョン情報表示とCSV出力・取込を行う設定ダイアログ</summary>
public class SettingsDialog : Form
{
    private readonly Action _onDataImported;

    public SettingsDialog(Action onDataImported)
    {
        _onDataImported = onDataImported;

        Text = Constants.ButtonSettings;
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        // ボタンが切れないよう、高さは中身に合わせる（画面の拡大率で文字とボタンの大きさが変わるため）
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;

        var versionLabel = new Label
        {
            Text = $"LDTPapp\nバージョン: {Constants.AppVersion}\n最終更新日: {Constants.AppLastUpdated}",
            AutoSize = true,
            Margin = new Padding(3, 3, 3, 12),
        };

        var layout = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(15),
        };
        layout.Controls.Add(versionLabel);
        layout.Controls.Add(CreateButton(Constants.ButtonCsvExport, ExportCsv));
        layout.Controls.Add(CreateButton(Constants.ButtonCsvImport, ImportCsv));
        layout.Controls.Add(CreateButton(Constants.ButtonClose, (_, _) => Close()));

        Controls.Add(layout);
    }

    private void ExportCsv(object? sender, EventArgs e)
    {
        string fileName;
        try
        {
            fileName = DataExportService.ExportToCsv(AppConfig.ExportFolder);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(Constants.MsgCsvExportErrorFormat, ex.Message), Text);
            return;
        }

        MessageBox.Show(this, string.Format(Constants.MsgCsvExportedFormat, fileName), Text);
        Process.Start(new ProcessStartInfo(AppConfig.ExportFolder) { UseShellExecute = true });
        Close();
    }

    private void ImportCsv(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog { Filter = "CSVファイル (*.csv)|*.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            DataExportService.ImportFromCsv(dialog.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, string.Format(Constants.MsgCsvImportErrorFormat, ex.Message), Text);
            return;
        }

        MessageBox.Show(this, Constants.MsgCsvImported, Text);
        _onDataImported();
    }
}

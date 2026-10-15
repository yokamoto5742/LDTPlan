using System.Text;
using LDTPlan.Data;
using LDTPlan.Forms;
using LDTPlan.Services;
using LDTPlan.Utils;
using Microsoft.Data.Sqlite;

namespace LDTPlan;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        StartupLog.Mark("Main開始");

        // Shift_JIS を使用可能にする
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ApplicationConfiguration.Initialize();

        if (!File.Exists(AppConfig.ConfigPath))
        {
            MessageBox.Show(
                string.Format(Constants.MsgConfigNotFoundFormat, AppConfig.ConfigPath),
                Constants.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        AppConfig.Load();
        StartupLog.Mark("設定読込完了");

        // メイン画面はDB初期化とデータ読込の後に表示するので、それまでの間は起動中であることを示す
        var splash = new SplashForm();
        splash.Show();
        splash.Update();


        DbInitializer.Initialize();
        StartupLog.Mark("DB初期化完了");

        // 起動を待たせないよう、バックアップは裏で行う。失敗しても次回起動時に再試行されるので無視する
        Task.Run(() =>
        {
            try
            {
                DatabaseBackupService.BackupIfNeeded(DateTime.Now);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SqliteException)
            {
            }
        });

        // 患者CSVが存在しない場合は起動しない（Python版と同じ挙動）
        if (!File.Exists(AppConfig.PatientDataPath))
            return;

        var mainForm = new MainForm();
        mainForm.Shown += (_, _) => splash.Close();
        StartupLog.Mark("画面生成完了");
        Application.Run(mainForm);
    }
}

using System.Diagnostics;
using System.Runtime.InteropServices;
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

        // 二重起動を防ぐ。既に起動していれば、その画面を前面に出して終了する
        using var mutex = new Mutex(true, "LDTPlan_SingleInstance", out var createdNew);
        if (!createdNew)
        {
            ActivateRunningInstance();
            return;
        }

        // Shift_JIS を使用可能にする
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ApplicationConfiguration.Initialize();

        if (!File.Exists(AppConfig.ConfigPath))
        {
            MessageDialog.Show(null,
                string.Format(Constants.MsgConfigNotFoundFormat, AppConfig.ConfigPath),
                Constants.AppTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        AppConfig.Load();
        StartupLog.Mark("設定読込完了");

        // 全画面の文字の大きさを config.ini の font_size にそろえる（最初の画面を作る前に呼ぶ必要がある）。
        // 先に Control.DefaultFont を参照すると既定フォントが確定して設定が効かなくなるため、元のフォントから書体を取る。
        // WinForms は「テキストのサイズ」の倍率で既定フォントを拡大し、ピクセル固定の画面から文字がはみ出すため、倍率で割って打ち消す
        Application.SetDefaultFont(new Font(SystemFonts.MessageBoxFont!.FontFamily, AppConfig.FontSize / SystemTextScale.Factor));

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

    private const int SwRestore = 9;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    /// <summary>起動済みのアプリの画面を前面に出す（最小化されていれば元に戻す）</summary>
    private static void ActivateRunningInstance()
    {
        using var current = Process.GetCurrentProcess();
        foreach (var process in Process.GetProcessesByName(current.ProcessName))
        {
            using (process)
            {
                if (process.Id == current.Id || process.MainWindowHandle == IntPtr.Zero)
                    continue;

                if (IsIconic(process.MainWindowHandle))
                    ShowWindow(process.MainWindowHandle, SwRestore);
                SetForegroundWindow(process.MainWindowHandle);
                return;
            }
        }
    }
}

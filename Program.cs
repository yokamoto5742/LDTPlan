using System.Text;
using LDTPlan.Data;
using LDTPlan.Forms;
using LDTPlan.Utils;

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
        DbInitializer.Initialize();
        StartupLog.Mark("DB初期化完了");

        // 患者CSVが存在しない場合は起動しない（Python版と同じ挙動）
        if (!File.Exists(AppConfig.PatientDataPath))
            return;

        var mainForm = new MainForm();
        StartupLog.Mark("画面生成完了");
        Application.Run(mainForm);
    }
}

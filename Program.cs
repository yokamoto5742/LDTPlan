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
        DbInitializer.Initialize();

        // 患者CSVが存在しない場合は起動しない（Python版と同じ挙動）
        if (!File.Exists(AppConfig.PatientDataPath))
            return;

        Application.Run(new MainForm());
    }
}

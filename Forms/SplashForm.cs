namespace LDTPlan.Forms;

/// <summary>メイン画面がデータを読み込み終えるまで表示する「起動中」表示（入力欄を持たない）</summary>
public class SplashForm : Form
{
    public SplashForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = false;
        // 起動に時間がかかる間に他のアプリを操作しても、起動中であることが隠れないようにする
        TopMost = true;
        ClientSize = new Size(240, 80);
        Controls.Add(new Label
        {
            Text = Constants.MsgStarting,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            BorderStyle = BorderStyle.FixedSingle,
        });
    }
}

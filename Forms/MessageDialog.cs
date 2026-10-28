namespace LDTPlan.Forms;

/// <summary>
/// アプリの文字の大きさで表示するメッセージボックス。
/// 標準の MessageBox は Windows のシステムフォント固定で font_size が効かないため、代わりに使う
/// </summary>
internal static class MessageDialog
{
    public static DialogResult Show(
        IWin32Window? owner, string text, string caption,
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        MessageBoxIcon icon = MessageBoxIcon.None,
        MessageBoxDefaultButton defaultButton = MessageBoxDefaultButton.Button1,
        int autoCloseMilliseconds = 0)
    {
        using var form = new Form
        {
            Text = caption,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = owner == null ? FormStartPosition.CenterScreen : FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(UiFactory.ScaleToFont(12)),
        };

        var layout = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 2,
        };

        var sysIcon = ToSystemIcon(icon);
        if (sysIcon != null)
        {
            layout.Controls.Add(new PictureBox
            {
                Image = sysIcon.ToBitmap(),
                SizeMode = PictureBoxSizeMode.AutoSize,
                Margin = new Padding(0, 0, UiFactory.ScaleToFont(12), 0),
            }, 0, 0);
        }

        layout.Controls.Add(new Label
        {
            Text = text,
            AutoSize = true,
            MaximumSize = new Size(UiFactory.ScaleToFont(480), 0),
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, UiFactory.ScaleToFont(4), 0, UiFactory.ScaleToFont(4)),
        }, 1, 0);

        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0, UiFactory.ScaleToFont(16), 0, 0),
        };
        layout.Controls.Add(buttonPanel, 0, 1);
        layout.SetColumnSpan(buttonPanel, 2);

        // 自動で閉じるときはボタンを置かず、指定時間で閉じる
        var autoClose = autoCloseMilliseconds > 0;
        var defs = autoClose ? [] : ButtonDefinitions(buttons);
        var defaultIndex = Math.Min((int)defaultButton / 256, defs.Count - 1);
        var created = new List<Button>();
        // 右から並べるため、定義の末尾から追加する
        for (var i = defs.Count - 1; i >= 0; i--)
        {
            var (label, result) = defs[i];
            var button = new Button
            {
                Text = label,
                DialogResult = result,
                AutoSize = true,
                MinimumSize = new Size(UiFactory.ScaleToFont(80), 0),
            };
            buttonPanel.Controls.Add(button);
            created.Add(button);
            if (i == defaultIndex)
                form.AcceptButton = button;
        }

        // Esc と × ボタンは最後のボタン（OK または いいえ）の扱いにする
        if (created.Count > 0)
            form.CancelButton = created.First();

        // ボタンがないときは余白も出さない
        buttonPanel.Visible = created.Count > 0;

        form.Controls.Add(layout);
        form.Shown += (_, _) => (form.AcceptButton as Button)?.Focus();

        using var timer = new System.Windows.Forms.Timer { Interval = autoClose ? autoCloseMilliseconds : 1 };
        timer.Tick += (_, _) => form.Close();
        form.Shown += (_, _) => timer.Enabled = autoClose;
        return owner == null ? form.ShowDialog() : form.ShowDialog(owner);
    }

    private static Icon? ToSystemIcon(MessageBoxIcon icon) => icon switch
    {
        MessageBoxIcon.Error => SystemIcons.Error,
        MessageBoxIcon.Warning => SystemIcons.Warning,
        MessageBoxIcon.Information => SystemIcons.Information,
        MessageBoxIcon.Question => SystemIcons.Question,
        _ => null,
    };

    private static List<(string Label, DialogResult Result)> ButtonDefinitions(MessageBoxButtons buttons) => buttons switch
    {
        MessageBoxButtons.YesNo => [(Constants.ButtonYes, DialogResult.Yes), (Constants.ButtonNo, DialogResult.No)],
        _ => [(Constants.ButtonOk, DialogResult.OK)],
    };
}

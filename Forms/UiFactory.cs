using LDTPlan.Utils;

namespace LDTPlan.Forms;

/// <summary>フォーム共通のコントロール生成ヘルパー</summary>
internal static class UiFactory
{
    /// <summary>9pt の文字に合わせて決めた寸法を、設定したフォントサイズに比例して拡大する</summary>
    public static int ScaleToFont(int size) => (int)Math.Round(size * Control.DefaultFont.Size / 9f);

    /// <summary>見出しのフォントサイズ（本文と同じく Windows の文字サイズ設定の倍率を掛ける）</summary>
    public static float HeadingFontSize => AppConfig.HeadingFontSize * Control.DefaultFont.Size / AppConfig.FontSize;

    public static Label CreateLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(8, 0, 2, 0),
    };

    // 行内のコントロールは Anchor = Left と上下対称の余白で、行の高さの中央にそろえる
    public static TextBox CreateTextBox(int width, bool readOnly = false) => new()
    {
        Width = ScaleToFont(width),
        ReadOnly = readOnly,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(3, 4, 3, 4),
    };

    /// <summary>一番長い選択肢が切れない幅のコンボボックスを作る</summary>
    public static ComboBox CreateCombo(IEnumerable<string> items)
    {
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 4, 3, 4),
        };
        combo.Items.AddRange(items.Cast<object>().ToArray());
        var textWidth = combo.Items.Cast<string>()
            .Select(item => TextRenderer.MeasureText(item, combo.Font).Width)
            .DefaultIfEmpty(0)
            .Max();
        combo.Width = textWidth + SystemInformation.VerticalScrollBarWidth + 8;
        return combo;
    }

    public static CheckBox CreateCheckBox(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(8, 4, 8, 4),
    };

    public static Button CreateButton(string text, EventHandler onClick)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 4, 3, 4),
            Padding = new Padding(6, 2, 6, 2),
        };
        button.Click += onClick;
        return button;
    }

    public static FlowLayoutPanel CreateRow(params Control[] controls)
    {
        var panel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0),
        };
        panel.Controls.AddRange(controls);
        return panel;
    }

    /// <summary>選択のみ可のコンボボックスに値を設定（一覧にない値は追加して選択）</summary>
    public static void SetComboValue(ComboBox combo, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            combo.SelectedIndex = -1;
            return;
        }
        if (!combo.Items.Contains(value))
            combo.Items.Add(value);
        combo.SelectedItem = value;
    }

    public static string GetComboValue(ComboBox combo) => combo.SelectedItem?.ToString() ?? "";

    public static void ReplaceComboItems(ComboBox combo, IEnumerable<string> items)
    {
        combo.Items.Clear();
        combo.Items.AddRange(items.Cast<object>().ToArray());
        combo.SelectedIndex = -1;
    }
}

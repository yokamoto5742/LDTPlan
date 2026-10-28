using System.Globalization;

namespace LDTPlan.Utils;

/// <summary>ホーム画面を閉じた位置を端末ごとに保存し、次回起動時に読み出す</summary>
public static class WindowPositionStore
{
    // 画面が他の端末と共有されないよう、config.ini ではなくユーザーごとのローカルフォルダに置く
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LDTPlan", "window_position.txt");

    /// <summary>保存した位置を返す。ファイルがない・読めない場合は null</summary>
    public static Point? Load(string path)
    {
        try
        {
            var parts = File.ReadAllText(path).Split(',');
            if (parts.Length == 2
                && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)
                && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var y))
                return new Point(x, y);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }

        return null;
    }

    /// <summary>位置を保存する。書き込めなくても起動に支障はないので無視する</summary>
    public static void Save(string path, Point location)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Create(CultureInfo.InvariantCulture, $"{location.X},{location.Y}"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// 画面の上端（タイトルバー）が、いずれかのモニターに掛かっているか。
    /// モニターの取り外しや解像度の変更で、保存した位置が画面外になっていないかの確認に使う
    /// </summary>
    public static bool IsVisible(Point location, Size size)
    {
        const int titleBarHeight = 40;
        var titleBar = new Rectangle(location, new Size(size.Width, titleBarHeight));
        return Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(titleBar));
    }
}

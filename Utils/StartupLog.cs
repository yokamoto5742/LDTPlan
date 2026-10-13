using System.Diagnostics;

namespace LDTPlan.Utils;

/// <summary>起動時間の内訳を計測し、exeと同じフォルダの startup.log に出力する</summary>
public static class StartupLog
{
    private static readonly DateTime ProcessStart = Process.GetCurrentProcess().StartTime;
    private static readonly List<string> Lines = [];

    public static string LogPath => Path.Combine(AppContext.BaseDirectory, "startup.log");

    /// <summary>プロセス開始からの経過時間を記録する</summary>
    public static void Mark(string label)
        => Lines.Add($"{(DateTime.Now - ProcessStart).TotalMilliseconds,6:F0} ms  {label}");

    /// <summary>記録した内容をファイルへ書き出す（直近1回分で上書き）</summary>
    public static void Flush()
    {
        if (!AppConfig.StartupLogEnabled)
            return;

        try
        {
            File.WriteAllLines(LogPath, Lines);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // 計測ログが書けなくてもアプリの動作は継続する
        }
    }
}

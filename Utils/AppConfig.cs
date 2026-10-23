namespace LDTPlan.Utils;

/// <summary>config.ini の読み込みと設定値へのアクセスを提供する</summary>
public static class AppConfig
{
    private static readonly Dictionary<string, Dictionary<string, string>> Sections =
        new(StringComparer.OrdinalIgnoreCase);

    public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "config.ini");

    public static void Load()
    {
        Sections.Clear();
        var currentSection = "";
        foreach (var rawLine in File.ReadAllLines(ConfigPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
                continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                currentSection = line[1..^1];
                Sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex > 0 && Sections.TryGetValue(currentSection, out var section))
                section[line[..separatorIndex].Trim()] = line[(separatorIndex + 1)..].Trim();
        }
    }

    private static string Get(string section, string key, string fallback = "")
        => Sections.TryGetValue(section, out var s) && s.TryGetValue(key, out var v) ? v : fallback;

    private static int GetInt(string section, string key, int fallback)
        => int.TryParse(Get(section, key), out var v) ? v : fallback;

    private static bool GetBool(string section, string key, bool fallback)
        => bool.TryParse(Get(section, key), out var v) ? v : fallback;

    // 設定値（キー名と既定値はここだけに書く）
    public static string DbPath
    {
        get
        {
            // "sqlite:///ldtp_app.db" 形式のURLからファイルパスを取り出す
            var url = Get("Database", "db_url", "sqlite:///ldtp_app.db");
            const string prefix = "sqlite:///";
            var path = url.StartsWith(prefix, StringComparison.Ordinal) ? url[prefix.Length..] : url;
            // 相対パスは作業ディレクトリではなくexeのフォルダ基準にする（絶対パスはそのまま使われる）
            return Path.Combine(AppContext.BaseDirectory, path);
        }
    }

    public static string TemplatePath => Get("Paths", "template_path");
    public static string OutputPath => Get("Paths", "output_path");
    public static string PatientDataPath => Get("FilePaths", "patient_data");
    public static string ExportFolder => Get("FilePaths", "export_folder");
    public static string ManualPdfPath => Get("FilePaths", "manual_pdf");
    public static string DocumentNumber => Get("Document", "document_number");
    public static bool StartupLogEnabled => GetBool("settings", "startup_log", true);
    public static string BackupDir => Get("Backup", "backup_dir");
    public static int BackupRetentionDays => GetInt("Backup", "retention_days", 7);

    public static int WindowWidth => GetInt("settings", "window_width", 1200);
    public static int WindowHeight => GetInt("settings", "window_height", 900);

    /// <summary>画面の文字のフォントサイズ（config.ini のピクセル値をポイントに換算して返す）</summary>
    public static float FontSize => GetInt("UI", "font_size", 16) * 0.75f;

    /// <summary>見出しのフォントサイズ（config.ini のピクセル値をポイントに換算して返す）</summary>
    public static float HeadingFontSize => GetInt("UI", "heading_font_size", 16) * 0.75f;

    public static int BarcodeImageWidth => GetInt("Barcode", "image_width", 200);
    public static int BarcodeImageHeight => GetInt("Barcode", "image_height", 30);
    public static int BarcodeQuietZone => GetInt("Barcode", "quiet_zone", 1);
    public static string BarcodeImagePosition => Get("Barcode", "image_position", "B2");
}

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

    public static string Get(string section, string key, string fallback = "")
        => Sections.TryGetValue(section, out var s) && s.TryGetValue(key, out var v) ? v : fallback;

    public static int GetInt(string section, string key, int fallback)
        => int.TryParse(Get(section, key), out var v) ? v : fallback;

    public static bool GetBool(string section, string key, bool fallback)
        => bool.TryParse(Get(section, key), out var v) ? v : fallback;

    // 主要設定値
    public static string DbPath
    {
        get
        {
            // "sqlite:///ldtp_app.db" 形式のURLからファイルパスを取り出す
            var url = Get("Database", "db_url", "sqlite:///ldtp_app.db");
            const string prefix = "sqlite:///";
            var path = url.StartsWith(prefix) ? url[prefix.Length..] : url;
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
}

using System.Text;
using LDTPlan.Data;
using LDTPlan.Utils;
using Microsoft.Data.Sqlite;

// AppConfig が静的な設定を保持するため、テストは並列実行しない
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace LDTPlan.Tests;

/// <summary>一時フォルダを指す config.ini と空のDBを用意する（テストごとに生成して破棄する）</summary>
public sealed class TestEnvironment : IDisposable
{
    // Shift_JIS を使用可能にする（アプリ本体は Program.Main で登録している）
    static TestEnvironment() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    // 公開リポジトリに含めない配布先のExcelテンプレート（読み込むだけで書き換えない）
    public const string TemplatePath = @"C:\Shinseikai\LDTPlan\LDTPform.xlsm";

    public string Root { get; } = Path.Combine(Path.GetTempPath(), "LDTPlanTests_" + Guid.NewGuid().ToString("N"));
    public string DbPath => Path.Combine(Root, "test.db");
    public string PatientCsvPath => Path.Combine(Root, "pat.csv");
    public string ExportFolder => Path.Combine(Root, "export");
    public string OutputFolder => Path.Combine(Root, "output");

    public TestEnvironment()
    {
        Directory.CreateDirectory(Root);
        WriteConfig($"db_url = sqlite:///{DbPath}");
        DbInitializer.Initialize();
    }

    /// <summary>[Database] セクションの内容を指定して config.ini を書き直し、再読込する</summary>
    public void WriteConfig(string databaseSection, string extraSections = "")
    {
        File.WriteAllText(AppConfig.ConfigPath, $"""
            [Database]
            {databaseSection}

            [Paths]
            template_path = {TemplatePath}
            output_path = {OutputFolder}

            [FilePaths]
            patient_data = {PatientCsvPath}
            export_folder = {ExportFolder}

            [Document]
            document_number = 39221

            {extraSections}
            """);
        AppConfig.Load();
    }

    public void WritePatientCsv(params string[] lines)
        => File.WriteAllLines(PatientCsvPath, lines, Encoding.GetEncoding("shift_jis"));

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        Directory.Delete(Root, recursive: true);
    }
}

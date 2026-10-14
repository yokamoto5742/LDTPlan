using LDTPlan.Utils;

namespace LDTPlan.Tests;

public sealed class AppConfigTests : IDisposable
{
    private readonly TestEnvironment _env = new();

    public void Dispose() => _env.Dispose();

    [Fact]
    public void 相対パスのDBはexeのフォルダ基準で解決する()
    {
        _env.WriteConfig("db_url = sqlite:///ldtp_app.db");

        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "ldtp_app.db"), AppConfig.DbPath);
    }

    [Fact]
    public void DBのパスは作業ディレクトリが変わっても変わらない()
    {
        _env.WriteConfig("db_url = sqlite:///ldtp_app.db");
        var original = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = _env.Root;

            Assert.Equal(Path.Combine(AppContext.BaseDirectory, "ldtp_app.db"), Path.GetFullPath(AppConfig.DbPath));
        }
        finally
        {
            Environment.CurrentDirectory = original;
        }
    }

    [Fact]
    public void 絶対パスのDBはそのまま使う()
    {
        _env.WriteConfig($"db_url = sqlite:///{_env.DbPath}");

        Assert.Equal(_env.DbPath, AppConfig.DbPath);
    }

    [Fact]
    public void 設定値を読み込む()
    {
        _env.WriteConfig("", """
            [settings]
            window_width = 1300
            window_height =900
            startup_log = false

            [UI]
            heading_font_size = 20

            [Barcode]
            quiet_zone = 2
            image_width = 300
            image_height = 40
            image_position = C3
            """);

        Assert.Equal(1300, AppConfig.WindowWidth);
        Assert.Equal(900, AppConfig.WindowHeight);
        Assert.False(AppConfig.StartupLogEnabled);
        Assert.Equal(15f, AppConfig.HeadingFontSize); // 20px = 15pt
        Assert.Equal(2, AppConfig.BarcodeQuietZone);
        Assert.Equal(300, AppConfig.BarcodeImageWidth);
        Assert.Equal(40, AppConfig.BarcodeImageHeight);
        Assert.Equal("C3", AppConfig.BarcodeImagePosition);
    }

    [Fact]
    public void 設定がない項目は既定値を使う()
    {
        _env.WriteConfig("");

        Assert.Equal(1200, AppConfig.WindowWidth);
        Assert.Equal(900, AppConfig.WindowHeight);
        Assert.True(AppConfig.StartupLogEnabled);
        Assert.Equal(12f, AppConfig.HeadingFontSize);
        Assert.Equal(1, AppConfig.BarcodeQuietZone);
        Assert.Equal(200, AppConfig.BarcodeImageWidth);
        Assert.Equal(30, AppConfig.BarcodeImageHeight);
        Assert.Equal("B2", AppConfig.BarcodeImagePosition);
    }

    [Fact]
    public void 配布するconfig_iniのキーはすべて読み込まれる()
    {
        // リポジトリ直下の config.ini（テスト実行フォルダから見て5階層上）
        var repositoryConfig = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "config.ini"));
        File.Copy(repositoryConfig, AppConfig.ConfigPath, overwrite: true);
        AppConfig.Load();

        Assert.Equal(1300, AppConfig.WindowWidth);
        Assert.Equal(12f, AppConfig.HeadingFontSize);
        Assert.Equal(200, AppConfig.BarcodeImageWidth);
        Assert.Equal("B2", AppConfig.BarcodeImagePosition);
        Assert.Equal("39221", AppConfig.DocumentNumber);
        Assert.EndsWith("LDTPform.xlsm", AppConfig.TemplatePath);
        Assert.Equal(7, AppConfig.BackupRetentionDays);
    }

    [Fact]
    public void db_urlがない場合は既定のファイル名を使う()
    {
        _env.WriteConfig("");

        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "ldtp_app.db"), AppConfig.DbPath);
    }
}

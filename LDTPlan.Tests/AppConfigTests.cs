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
    public void db_urlがない場合は既定のファイル名を使う()
    {
        _env.WriteConfig("");

        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "ldtp_app.db"), AppConfig.DbPath);
    }
}

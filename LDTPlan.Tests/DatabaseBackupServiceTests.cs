using LDTPlan.Services;
using Microsoft.Data.Sqlite;

namespace LDTPlan.Tests;

public sealed class DatabaseBackupServiceTests : IDisposable
{
    private readonly TestEnvironment _env = new();
    private string BackupDir => Path.Combine(_env.Root, "backup");

    public DatabaseBackupServiceTests()
        => _env.WriteConfig($"db_url = sqlite:///{_env.DbPath}", $"""
            [Backup]
            backup_dir = {BackupDir}
            retention_days = 7
            """);

    public void Dispose() => _env.Dispose();

    private string[] BackupFileNames()
        => Directory.GetFiles(BackupDir).Select(Path.GetFileName).Order().ToArray()!;

    [Fact]
    public void タイムスタンプ付きのファイル名でバックアップする()
    {
        DatabaseBackupService.BackupIfNeeded(new DateTime(2026, 10, 14, 8, 30, 15));

        Assert.Equal(["test_20261014_083015.db"], BackupFileNames());
    }

    [Fact]
    public void バックアップしたファイルからマスタを読める()
    {
        DatabaseBackupService.BackupIfNeeded(new DateTime(2026, 10, 14, 8, 30, 15));

        using var connection = new SqliteConnection(
            $"Data Source={Path.Combine(BackupDir, "test_20261014_083015.db")};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM main_diseases";
        Assert.True((long)command.ExecuteScalar()! > 0);
    }

    [Fact]
    public void 当日分があれば作成しない()
    {
        DatabaseBackupService.BackupIfNeeded(new DateTime(2026, 10, 14, 8, 30, 15));
        DatabaseBackupService.BackupIfNeeded(new DateTime(2026, 10, 14, 17, 0, 0));

        Assert.Equal(["test_20261014_083015.db"], BackupFileNames());
    }

    [Fact]
    public void 保存期間を過ぎたバックアップを削除する()
    {
        Directory.CreateDirectory(BackupDir);
        File.WriteAllText(Path.Combine(BackupDir, "test_20261007_083000.db"), "");
        File.WriteAllText(Path.Combine(BackupDir, "test_20261007_090000.db"), "");
        File.WriteAllText(Path.Combine(BackupDir, "other.db"), "");

        DatabaseBackupService.BackupIfNeeded(new DateTime(2026, 10, 14, 8, 45, 0));

        Assert.Equal(["other.db", "test_20261007_090000.db", "test_20261014_084500.db"], BackupFileNames());
    }

    [Fact]
    public void バックアップ先が未設定なら何もしない()
    {
        _env.WriteConfig($"db_url = sqlite:///{_env.DbPath}");

        DatabaseBackupService.BackupIfNeeded(new DateTime(2026, 10, 14, 8, 30, 15));

        Assert.False(Directory.Exists(BackupDir));
    }
}

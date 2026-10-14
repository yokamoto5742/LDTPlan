using System.Globalization;
using LDTPlan.Utils;
using Microsoft.Data.Sqlite;

namespace LDTPlan.Services;

/// <summary>DBを1日1回バックアップフォルダへ保存し、保存期間を過ぎたバックアップを削除する</summary>
public static class DatabaseBackupService
{
    private const string TimestampFormat = "yyyyMMdd_HHmmss";

    /// <summary>当日分のバックアップがなければ作成する（backup_dir が未設定なら何もしない）</summary>
    public static void BackupIfNeeded(DateTime now)
    {
        var backupDir = AppConfig.BackupDir;
        if (backupDir.Length == 0)
            return;

        Directory.CreateDirectory(backupDir);
        var prefix = Path.GetFileNameWithoutExtension(AppConfig.DbPath) + "_";
        var backups = Directory.GetFiles(backupDir, prefix + "*.db")
            .Select(path => (Path: path, Time: ParseTimestamp(path, prefix)))
            .Where(b => b.Time.HasValue)
            .ToList();

        if (!backups.Any(b => b.Time!.Value.Date == now.Date))
            CreateBackup(Path.Combine(backupDir, prefix + now.ToString(TimestampFormat) + ".db"));

        var limit = now.AddDays(-AppConfig.BackupRetentionDays);
        foreach (var old in backups.Where(b => b.Time < limit))
            File.Delete(old.Path);
    }

    private static DateTime? ParseTimestamp(string path, string prefix)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        return DateTime.TryParseExact(name[prefix.Length..], TimestampFormat,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var time) ? time : null;
    }

    private static void CreateBackup(string backupPath)
    {
        // 書き込み中でも整合性のあるファイルにするため、ファイルコピーではなくSQLiteのバックアップ機能を使う。
        // 途中で終了しても不完全なファイルが当日分と見なされないよう、一時ファイルに書いてから名前を変える
        var tempPath = backupPath + ".tmp";
        using (var source = new SqliteConnection($"Data Source={AppConfig.DbPath};Mode=ReadOnly;Pooling=False"))
        using (var destination = new SqliteConnection($"Data Source={tempPath};Pooling=False"))
        {
            source.Open();
            destination.Open();
            source.BackupDatabase(destination);
        }
        File.Move(tempPath, backupPath, overwrite: true);
    }
}

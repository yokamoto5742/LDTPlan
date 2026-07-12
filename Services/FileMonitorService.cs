using LDTPlan.Utils;

namespace LDTPlan.Services;

/// <summary>pat.csv を監視し、削除されたらアプリを終了する</summary>
public sealed class FileMonitorService : IDisposable
{
    private readonly FileSystemWatcher _watcher;

    public FileMonitorService(Form mainForm)
    {
        var csvPath = AppConfig.PatientDataPath;
        _watcher = new FileSystemWatcher(
            Path.GetDirectoryName(csvPath) ?? ".",
            Path.GetFileName(csvPath));
        _watcher.Deleted += (_, _) => mainForm.BeginInvoke(mainForm.Close);
        _watcher.EnableRaisingEvents = true;
    }

    public void Dispose() => _watcher.Dispose();
}

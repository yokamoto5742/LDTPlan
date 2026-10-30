using LDTPlan.Utils;
using System.Drawing;

namespace LDTPlan.Tests;

public class WindowPositionStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void 保存した位置を読み出せる()
    {
        var path = Path.Combine(_dir, "sub", "window_position.txt");

        WindowPositionStore.Save(path, new Point(-120, 80));

        Assert.Equal(new Point(-120, 80), WindowPositionStore.Load(path));
    }

    [Fact]
    public void ファイルがなければnullを返す()
    {
        Assert.Null(WindowPositionStore.Load(Path.Combine(_dir, "none.txt")));
    }

    [Fact]
    public void 内容が壊れていればnullを返す()
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "window_position.txt");
        File.WriteAllText(path, "abc");

        Assert.Null(WindowPositionStore.Load(path));
    }

    [Fact]
    public void どのモニターにも掛からない位置は画面外と判定する()
    {
        Assert.False(WindowPositionStore.IsVisible(new Point(-30000, -30000), new Size(1000, 800)));
    }
}

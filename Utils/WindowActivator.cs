using System.Runtime.InteropServices;

namespace LDTPlan.Utils;

/// <summary>画面を他のアプリより手前に出し、キー入力を受け取れる状態にする</summary>
public static class WindowActivator
{
    private const int SwRestore = 9;
    private static readonly IntPtr HwndTopmost = new(-1);
    private static readonly IntPtr HwndNoTopmost = new(-2);
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint FlashwAll = 0x0003;
    private const uint FlashwTimerNoForeground = 0x000C;

    [StructLayout(LayoutKind.Sequential)]
    private struct FlashWindowInfo
    {
        public uint Size;
        public IntPtr HWnd;
        public uint Flags;
        public uint Count;
        public uint Timeout;
    }

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr processId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool FlashWindowEx(ref FlashWindowInfo info);

    /// <summary>画面を最前面に出す（最小化されていれば元に戻す）。前面に出せなかったときはタスクバーを点滅させて false を返す</summary>
    public static bool BringToFront(IntPtr hWnd)
    {
        if (IsIconic(hWnd))
            ShowWindow(hWnd, SwRestore);

        // 重なり順は前面化の制限を受けないので、一瞬だけ常に手前にして一番上へ上げる
        SetWindowPos(hWnd, HwndTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize);
        SetWindowPos(hWnd, HwndNoTopmost, 0, 0, 0, 0, SwpNoMove | SwpNoSize);

        // Windows は手前のアプリ以外からの前面化を拒むため、手前のアプリの入力スレッドに一時的につないで前面化する
        var foregroundThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
        var currentThread = GetCurrentThreadId();
        var attached = foregroundThread != 0 && foregroundThread != currentThread
            && AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            BringWindowToTop(hWnd);
            SetForegroundWindow(hWnd);
        }
        finally
        {
            if (attached)
                AttachThreadInput(currentThread, foregroundThread, false);
        }

        if (GetForegroundWindow() == hWnd)
            return true;

        // 前面に出せなかったときは、利用者が気づけるようタスクバーを点滅させる
        var info = new FlashWindowInfo
        {
            Size = (uint)Marshal.SizeOf<FlashWindowInfo>(),
            HWnd = hWnd,
            Flags = FlashwAll | FlashwTimerNoForeground,
        };
        FlashWindowEx(ref info);
        return false;
    }
}

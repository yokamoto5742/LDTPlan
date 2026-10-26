using Microsoft.Win32;

namespace LDTPlan.Utils;

/// <summary>Windows の「テキストのサイズ」（アクセシビリティ設定）の倍率</summary>
public static class SystemTextScale
{
    /// <summary>倍率（100% なら 1）。WinForms は Application.SetDefaultFont に渡したフォントをこの倍率で拡大する</summary>
    public static float Factor
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Accessibility");
            return key?.GetValue("TextScaleFactor") is int percent and > 0 ? percent / 100f : 1f;
        }
    }
}

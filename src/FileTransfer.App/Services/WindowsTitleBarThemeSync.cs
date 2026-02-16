using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Platform;

namespace FileTransfer.App.Services;

internal static class WindowsTitleBarThemeSync
{
    private const int DwmwaUseImmersiveDarkModeLegacy = 19;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaCaptionColor = 35;
    private const int DwmwaTextColor = 36;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpFrameChanged = 0x0020;
    private const uint RdwInvalidate = 0x0001;
    private const uint RdwFrame = 0x0400;
    private const uint RdwUpdatenow = 0x0100;
    private const int WmThemeChanged = 0x031A;
    private const int WmNcActivate = 0x0086;

    private static SetPreferredAppModeDelegate? s_setPreferredAppMode;
    private static AllowDarkModeForWindowDelegate? s_allowDarkModeForWindow;
    private static bool s_legacyFunctionsLoaded;
    private static bool s_preferredAppModeInitialized;

    // COLORREF uses 0x00BBGGRR.
    private const int LightCaptionColor = 0x00F3F3F3;
    private const int LightTextColor = 0x00151515;
    private const int DarkCaptionColor = 0x00202020;
    private const int DarkTextColor = 0x00FFFFFF;

    public static void Apply(Window? window, bool useDark)
    {
        if (!OperatingSystem.IsWindows() || window is null)
        {
            return;
        }

        IPlatformHandle? handle = window.TryGetPlatformHandle();
        if (handle is null || handle.Handle == IntPtr.Zero)
        {
            return;
        }

        int darkValue = useDark ? 1 : 0;
        int captionColor = useDark ? DarkCaptionColor : LightCaptionColor;
        int textColor = useDark ? DarkTextColor : LightTextColor;

        TryApplyLegacyWin10DarkMode(handle.Handle, useDark);

        // Win10 uses 19, newer builds use 20. Setting both is safe.
        _ = DwmSetWindowAttribute(handle.Handle, DwmwaUseImmersiveDarkModeLegacy, ref darkValue, sizeof(int));
        _ = DwmSetWindowAttribute(handle.Handle, DwmwaUseImmersiveDarkMode, ref darkValue, sizeof(int));
        _ = DwmSetWindowAttribute(handle.Handle, DwmwaCaptionColor, ref captionColor, sizeof(int));
        _ = DwmSetWindowAttribute(handle.Handle, DwmwaTextColor, ref textColor, sizeof(int));
        _ = SetWindowPos(handle.Handle, IntPtr.Zero, 0, 0, 0, 0, SwpNoSize | SwpNoMove | SwpNoZOrder | SwpFrameChanged);
        _ = SendMessage(handle.Handle, WmThemeChanged, IntPtr.Zero, IntPtr.Zero);
        _ = SendMessage(handle.Handle, WmNcActivate, (IntPtr)1, IntPtr.Zero);
        _ = RedrawWindow(handle.Handle, IntPtr.Zero, IntPtr.Zero, RdwInvalidate | RdwFrame | RdwUpdatenow);
        _ = DwmFlush();
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, IntPtr lpProcName);

    private static void TryApplyLegacyWin10DarkMode(IntPtr hwnd, bool useDark)
    {
        EnsureLegacyFunctionsLoaded();

        if (!s_preferredAppModeInitialized)
        {
            // Opt in once, then control dark/light per window.
            s_setPreferredAppMode?.Invoke(PreferredAppMode.AllowDark);
            s_preferredAppModeInitialized = true;
        }

        s_allowDarkModeForWindow?.Invoke(hwnd, useDark);
    }

    private static void EnsureLegacyFunctionsLoaded()
    {
        if (s_legacyFunctionsLoaded || !OperatingSystem.IsWindows())
        {
            return;
        }

        s_setPreferredAppMode = LoadUxThemeFunction<SetPreferredAppModeDelegate>(135);
        s_allowDarkModeForWindow = LoadUxThemeFunction<AllowDarkModeForWindowDelegate>(133);
        s_legacyFunctionsLoaded = true;
    }

    private static TDelegate? LoadUxThemeFunction<TDelegate>(int ordinal)
        where TDelegate : Delegate
    {
        IntPtr module = LoadLibrary("uxtheme.dll");
        if (module == IntPtr.Zero)
        {
            return null;
        }

        IntPtr proc = GetProcAddress(module, (IntPtr)ordinal);
        if (proc == IntPtr.Zero)
        {
            return null;
        }

        return Marshal.GetDelegateForFunctionPointer<TDelegate>(proc);
    }

    private enum PreferredAppMode
    {
        Default = 0,
        AllowDark = 1,
        ForceDark = 2,
        ForceLight = 3,
        Max = 4
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate PreferredAppMode SetPreferredAppModeDelegate(PreferredAppMode appMode);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool AllowDarkModeForWindowDelegate(IntPtr hWnd, [MarshalAs(UnmanagedType.Bool)] bool allow);
}

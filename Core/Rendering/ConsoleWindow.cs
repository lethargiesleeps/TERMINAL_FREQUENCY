using System.Runtime.InteropServices;

#pragma warning disable SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
namespace TERMINAL_FREQUENCY.Core.Rendering
{
    public static class ConsoleWindow
    {
        [DllImport("kernel32.dll")]
        private static extern nint GetConsoleWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(nint hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(nint hWnd);

        [DllImport("user32.dll")]
        private static extern int FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(nint hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(nint hWnd, nint hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(nint hWnd, uint crKey, byte bAlpha, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(nint hwnd, ref WindowCompositionAttributeData data);
        [DllImport("user32.dll")]
        private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [DllImport("dwmapi.dll")]
        private static extern void DwmExtendFrameIntoClientArea(nint hWnd, ref MARGINS margins);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(nint hwnd, uint dwAttribute, ref int pvAttribute, int cbAttribute);

        private static readonly nint HWND_TOPMOST = new nint(-1);
        private static readonly nint HWND_NOTOPMOST = new nint(-2);
        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        private const int GWL_STYLE = -16;
        private const int WS_CAPTION = 0x00C00000;
        private const int WS_THICKFRAME = 0x00040000;
        private const int WS_HSCROLL = 0x00100000;
        private const int WS_VSCROLL = 0x00200000;
        private const int WS_MAXIMIZEBOX = 0x00010000;
        private const int WS_MINIMIZEBOX = 0x00020000;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_LAYERED = 0x00080000;
        private const uint LWA_ALPHA = 0x00000002;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const uint WCA_ACCENT_POLICY = 19;
        private const uint FLASHW_ALL = 0x00000003;
        private const uint FLASHW_TIMERNOFG = 0x0000000C;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const uint DWMWA_BORDER_COLOR = 34;
        private const uint DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        private enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,  // Windows 10 1803+
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WindowCompositionAttributeData
        {
            public uint Attribute;
            public nint Data;
            public int SizeOfData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public nint hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MARGINS
        {
            public int Left, Right, Top, Bottom;
        }

        public static void SetWindowBlur(bool enable)
        {
            nint handle = GetConsoleWindow();

            var accent = new AccentPolicy();
            accent.AccentState = enable ? AccentState.ACCENT_ENABLE_BLURBEHIND : AccentState.ACCENT_DISABLED;

            var accentStructSize = Marshal.SizeOf(accent);
            nint accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }
        public static void SetClickThrough(bool enable)
        {
            nint handle = GetConsoleWindow();
            int exStyle = GetWindowLong(handle, GWL_EXSTYLE);

            //always keep the layered style
            exStyle |= WS_EX_LAYERED;

            if (enable)
                exStyle |= WS_EX_TRANSPARENT;  //clicks pass through
            else
                exStyle &= ~WS_EX_TRANSPARENT; //normal click behaviour

            SetWindowLong(handle, GWL_EXSTYLE, exStyle);
        }

        public static void SetWindowVibrancy(byte r, byte g, byte b, byte alpha = 0x99)
        {
            nint handle = GetConsoleWindow();

            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.AccentFlags = 2;
            accent.GradientColor = alpha << 24 | r << 16 | g << 8 | b;

            var accentStructSize = Marshal.SizeOf(accent);
            nint accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        public static void SetOpacity(byte opacity)
        {
            if (opacity < 0) opacity = 0;
            else if (opacity > 255) opacity = 255;

            nint handle = GetConsoleWindow();

            // Enable layered window
            int exStyle = GetWindowLong(handle, GWL_EXSTYLE);
            SetWindowLong(handle, GWL_EXSTYLE, exStyle | WS_EX_LAYERED);

            SetLayeredWindowAttributes(handle, 0, opacity, LWA_ALPHA);
        }

        public static void SetAlwaysOnTop(bool enable)
        {
            nint handle = GetConsoleWindow();
            nint position = enable ? HWND_TOPMOST : HWND_NOTOPMOST;
            SetWindowPos(handle, position, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        public static void DisableTitleBar()
        {
            nint handle = GetConsoleWindow();
            int style = GetWindowLong(handle, GWL_STYLE);
            style &= ~(WS_CAPTION | WS_MINIMIZEBOX | WS_MAXIMIZEBOX);
            SetWindowLong(handle, GWL_STYLE, style);
            ApplyStyle(handle);
            GetWindowRect(handle, out RECT rect);
            int newHeight = rect.Bottom - rect.Top + 60; //reclaim top
            SetWindowPos(handle, nint.Zero, rect.Left, rect.Top - 4, rect.Right - rect.Left, newHeight, SWP_NOZORDER);
        }

        public static void EnableTitleBar()
        {
            nint handle = GetConsoleWindow();
            int style = GetWindowLong(handle, GWL_STYLE);
            style |= WS_CAPTION | WS_MINIMIZEBOX | WS_MAXIMIZEBOX;
            SetWindowLong(handle, GWL_STYLE, style);
            ApplyStyle(handle);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static void DisableScrollBars()
        {
            nint handle = GetConsoleWindow();
            int style = GetWindowLong(handle, GWL_STYLE);
            style &= ~(WS_HSCROLL | WS_VSCROLL);
            SetWindowLong(handle, GWL_STYLE, style);
            ApplyStyle(handle);
            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
        }

        public static void EnableScrollBars()
        {
            nint handle = GetConsoleWindow();
            int style = GetWindowLong(handle, GWL_STYLE);
            style |= WS_HSCROLL | WS_VSCROLL;
            SetWindowLong(handle, GWL_STYLE, style);
            ApplyStyle(handle);
        }

        public static void DisableResize()
        {
            nint handle = GetConsoleWindow();
            int style = GetWindowLong(handle, GWL_STYLE);
            style &= ~(WS_THICKFRAME | WS_MAXIMIZEBOX);
            SetWindowLong(handle, GWL_STYLE, style);
            ApplyStyle(handle);
        }

        public static void EnableResize()
        {
            nint handle = GetConsoleWindow();
            int style = GetWindowLong(handle, GWL_STYLE);
            style |= WS_THICKFRAME | WS_MAXIMIZEBOX;
            SetWindowLong(handle, GWL_STYLE, style);
            ApplyStyle(handle);
        }

        //launch positioning
        public static void LaunchConsoleCenter()
        {
            nint handle = GetConsoleWindow();
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            GetWindowRect(handle, out RECT rect);
            int windowWidth = rect.Right - rect.Left;
            int windowHeight = rect.Bottom - rect.Top;

            int x = (screenWidth - windowWidth) / 2;
            int y = (screenHeight - windowHeight) / 2;

            SetWindowPos(handle, nint.Zero, x, y, 0, 0, SWP_NOZORDER | SWP_NOSIZE);
        }

        public static void LaunchConsoleAt(int x, int y)
        {
            nint handle = GetConsoleWindow();
            int screenWidth = GetSystemMetrics(SM_CXSCREEN);
            int screenHeight = GetSystemMetrics(SM_CYSCREEN);

            GetWindowRect(handle, out RECT rect);
            int windowWidth = rect.Right - rect.Left;
            int windowHeight = rect.Bottom - rect.Top;

            //clamp so at least part of the window is visible
            x = Math.Max(-windowWidth + 50, Math.Min(x, screenWidth - 50));
            y = Math.Max(0, Math.Min(y, screenHeight - 50));

            SetWindowPos(handle, nint.Zero, x, y, 0, 0, SWP_NOZORDER | SWP_NOSIZE);
        }

        //composite functions that use the individual ones
        public static void SetFullScreen()
        {
            ShowWindow(GetConsoleWindow(), 3); //SW_MAXIMIZE
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Validate platform compatibility", Justification = "<Pending>")]
        public static void SetScreenSize(int width, int height)
        {
            try
            {
                Console.SetWindowSize(width, height);
                Console.SetBufferSize(width, height);
            }
            catch (ArgumentOutOfRangeException)
            {
                //fallback: use largest valid size
                int maxWidth = Console.LargestWindowWidth;
                int maxHeight = Console.LargestWindowHeight;
                Console.SetWindowSize(maxWidth, maxHeight);
                Console.SetBufferSize(maxWidth, maxHeight);
            }
        }
        public static void ExclusiveMode(bool enable)
        {
            nint handle = GetConsoleWindow();
            nint taskbarHandle = FindWindow("Shell_TrayWnd", null);

            if (enable)
            {
                ShowWindow(taskbarHandle, SW_HIDE);

                int style = GetWindowLong(handle, GWL_STYLE);
                style &= ~(WS_CAPTION | WS_THICKFRAME);
                SetWindowLong(handle, GWL_STYLE, style);
                ApplyStyle(handle);

                ShowWindow(handle, 3); // SW_MAXIMIZE
                Thread.Sleep(100);
                Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);
                SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER);
                SetForegroundWindow(handle);
            }
            else
            {
                ShowWindow(handle, 1); // SW_NORMAL
                Thread.Sleep(100);

                int style = GetWindowLong(handle, GWL_STYLE);
                style |= WS_CAPTION | WS_THICKFRAME;
                SetWindowLong(handle, GWL_STYLE, style);
                ApplyStyle(handle);

                ShowWindow(taskbarHandle, SW_SHOW);
                Console.SetWindowSize(115, 35);
                Console.SetBufferSize(115, 35);
                Console.Clear();
                SetWindowPos(handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER);
            }
        }

        public static void FlashWindowOnBeat(int flashCount = 2)
        {
            nint handle = GetConsoleWindow();

            FLASHWINFO flashInfo = new FLASHWINFO();
            flashInfo.cbSize = (uint)Marshal.SizeOf(flashInfo);
            flashInfo.hwnd = handle;
            flashInfo.dwFlags = FLASHW_ALL | FLASHW_TIMERNOFG;
            flashInfo.uCount = (uint)flashCount;
            flashInfo.dwTimeout = 0;

            FlashWindowEx(ref flashInfo);
        }

        public static void SetWindowGlow(int radius, byte r, byte g, byte b)
        {
            nint handle = GetConsoleWindow();

            //extend frame
            MARGINS margins = new MARGINS();
            margins.Left = radius;
            margins.Right = radius;
            margins.Top = radius;
            margins.Bottom = radius;
            DwmExtendFrameIntoClientArea(handle, ref margins);

            //st the border color to create glow color
            int color = r << 16 | g << 8 | b;
            DwmSetWindowAttribute(handle, 34, ref color, sizeof(int)); // 34 = DWMWA_BORDER_COLOR

            //make the extended frame transparent to create glow illusion
            int useDarkMode = 1;
            DwmSetWindowAttribute(handle, 20, ref useDarkMode, sizeof(int)); // 20 = immersive dark mode
        }

        //TODO: function that launches app on specific monitor if there are multiple monitors
        private static void LaunchOnMonitor(int monitorIndex)
        {

        }

        //TODO: Rounded corners for Windows 11
        private static void SetWindowCorners()
        {

        }

        //TODO: some function that shakes the window OnSpike : SetWindowShake(intensity, duration)
        //TODO: exclusive mode, where task bar is hidden, like full screen video or game ExclusiveMode()
        //TODO: SetInertia() window slides to a stop when dragged
        //TODO: CycleMonitor(int direction) move window to next monitor
        //TODO: StreamToVirtualConsole() OBS support, super low priority
        //TODO: NetworkSync(int port) Allow two instance on difrent devices to sync over LAN
        //TODO: HotKeys(Dictionary<Keys, Action>) global hotkeys thatwork even if window isnt focused
        //TODO: MirrorMode() just flip the whole thing
        //TODO: ReactiveWindowSize() the whole window reacts via OnSpike hook
        private static void ApplyStyle(nint handle)
        {
            SetWindowPos(handle, nint.Zero, 0, 0, 0, 0,
                SWP_FRAMECHANGED | SWP_NOZORDER | SWP_NOMOVE | SWP_NOSIZE);
        }
    }
}
#pragma warning restore SYSLIB1054 // Use 'LibraryImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

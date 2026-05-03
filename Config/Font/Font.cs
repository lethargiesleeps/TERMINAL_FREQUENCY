using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TERMINAL_FREQUENCY.Config.Font
{
    public static class Font
    {
        [DllImport("kernel32.dll")]
        private static extern nint GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool SetCurrentConsoleFontEx(nint consoleOutput, bool maximumWindow, ref CONSOLE_FONT_INFO_EX consoleCurrentFont);

        [DllImport("kernel32.dll")]
        private static extern bool GetCurrentConsoleFontEx(nint consoleOutput, bool maximumWindow, ref CONSOLE_FONT_INFO_EX consoleCurrentFont);

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CONSOLE_FONT_INFO_EX
        {
            public uint cbSize;
            public uint nFont;
            public COORD dwFontSize;
            public int FontFamily;
            public int FontWeight;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string FaceName;
        }

        private const int STD_OUTPUT_HANDLE = -11;
        private static CONSOLE_FONT_INFO_EX _previousFont;
        private static bool _fontSaved = false;
        
        public static void SaveCurrentFont()
        {
            nint handle = GetStdHandle(STD_OUTPUT_HANDLE);
            _previousFont = new CONSOLE_FONT_INFO_EX();
            _previousFont.cbSize = (uint)Marshal.SizeOf(_previousFont);
            GetCurrentConsoleFontEx(handle, false, ref _previousFont);
            _fontSaved = true;
        }

        public static void DumpFontData()
        {
            nint handle = GetStdHandle(STD_OUTPUT_HANDLE);
            CONSOLE_FONT_INFO_EX currentFont = new CONSOLE_FONT_INFO_EX();
            currentFont.cbSize = (uint)Marshal.SizeOf(currentFont);
            GetCurrentConsoleFontEx(handle, false, ref currentFont);

            if (GetCurrentConsoleFontEx(handle, false, ref currentFont))
            {
                Debug.WriteLine("=== CURRENT FONT INFO ===");
                Debug.WriteLine($"  FaceName: {currentFont.FaceName}");
                Debug.WriteLine($"  FontSize: {currentFont.dwFontSize.X} x {currentFont.dwFontSize.Y} pixels");
                Debug.WriteLine($"  nFont (raster index): {currentFont.nFont}");
                Debug.WriteLine($"  FontFamily: {currentFont.FontFamily}");
                Debug.WriteLine($"  FontWeight: {currentFont.FontWeight}");
                Debug.WriteLine($"  cbSize: {currentFont.cbSize}");
                Debug.WriteLine("=========================");
            }
            else
            {
                Debug.WriteLine("Failed to get current font info");
            }
        }

        public static void SetRasterFont(int nFontIndex)
        {
            if (nFontIndex < 0) return;
            nint handle = GetStdHandle(STD_OUTPUT_HANDLE);
            CONSOLE_FONT_INFO_EX fontInfo = new CONSOLE_FONT_INFO_EX();
            fontInfo.cbSize = (uint)Marshal.SizeOf(fontInfo);
            fontInfo.nFont = (uint)nFontIndex;
            fontInfo.FaceName = "Terminal";
            SetCurrentConsoleFontEx(handle, false, ref fontInfo);
        }

        public static void SetCustomFont(FontFace fontFace, int fontSize, bool bold, string fontFaceOverride = "")
        {
            IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
            CONSOLE_FONT_INFO_EX fontInfo = new CONSOLE_FONT_INFO_EX();
            fontInfo.cbSize = (uint)Marshal.SizeOf(fontInfo);

            fontInfo.FaceName = (string.IsNullOrEmpty(fontFaceOverride) || string.IsNullOrWhiteSpace(fontFaceOverride))
                ? GetFontFaceName(fontFace) : fontFaceOverride;

            fontInfo.dwFontSize.Y = (short)fontSize;
            fontInfo.FontWeight = bold ? 700 : 400;
            fontInfo.FontFamily = 54; //FF_MODERN | TMPF_TRUETYPE | TMPF_FIXED_PITCH (0x36)

            GetCurrentConsoleFontEx(handle, false, ref fontInfo);
            float ratio = (float)fontInfo.dwFontSize.X / fontInfo.dwFontSize.Y;
            fontInfo.dwFontSize.X = (short)(fontSize * ratio);

            //clear raster font index!
            fontInfo.nFont = 0;

            bool result = SetCurrentConsoleFontEx(handle, false, ref fontInfo);

            if (!result)
            {
                fontInfo.FaceName = "Consolas";
                fontInfo.dwFontSize.Y = (short)fontSize;
                fontInfo.dwFontSize.X = (short)(fontSize * ratio);
                SetCurrentConsoleFontEx(handle, false, ref fontInfo);
            }
        }

        public static void SetRasterFont(RasterFontType rasterFontType)
        {
            nint handle = GetStdHandle(STD_OUTPUT_HANDLE);
            CONSOLE_FONT_INFO_EX fontInfo = new CONSOLE_FONT_INFO_EX();
            fontInfo.cbSize = (uint)Marshal.SizeOf(fontInfo);
            uint nFontIndex = 0;

            switch(rasterFontType)
            {
                case RasterFontType.FourBySix: nFontIndex = 0; break;
                case RasterFontType.SixByEight: nFontIndex = 2; break;
                case RasterFontType.EightByEight: nFontIndex = 4; break;            
                case RasterFontType.SixteenByEight: nFontIndex = 6; break;
                case RasterFontType.FiveByTwelve: nFontIndex = 8; break;
                case RasterFontType.SevenByTwelve: nFontIndex = 9; break;
                case RasterFontType.EightByTwelve: nFontIndex = 10; break;
                case RasterFontType.SixteenByTwelve: nFontIndex = 12; break;
                case RasterFontType.TwelveBySixteen: nFontIndex = 14; break;
                case RasterFontType.TenByEighteen: nFontIndex = 16; break;
                case RasterFontType.TenByTwenty: nFontIndex = 18; break;
                default: nFontIndex = 10; break;
            }

            fontInfo.nFont = nFontIndex;
            fontInfo.FaceName = "Terminal";
            SetCurrentConsoleFontEx(handle, false, ref fontInfo);
        }
        public static void RestorePreviousFont()
        {
            if (!_fontSaved) return;
            nint handle = GetStdHandle(STD_OUTPUT_HANDLE);
            SetCurrentConsoleFontEx(handle, false, ref _previousFont);
        }

        private static string GetFontFaceName(FontFace font)
        {
            return font switch
            {
                FontFace.CascadiaCode => "Cascadia Code",
                FontFace.CascadiaMono => "Cascadia Mono",
                FontFace.Consolas => "Consolas",
                FontFace.CourierNew => "Courier New",
                FontFace.LucidaConsole => "Lucida Console",
                FontFace.LucidaSansTypeWriter => "Lucida Sans Typewriter",
                FontFace.MSGothic => "MS Gothic",
                FontFace.NSimSun => "NSimSun",
                FontFace.Terminal => "Terminal",
                _ => "Consolas"
            };
        }
    }
}

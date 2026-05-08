using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using TERMINAL_FREQUENCY.Config.Settings;

#nullable disable warnings
namespace TERMINAL_FREQUENCY.Core.Rendering
{
    public class ScreenBuffer
    {
        private Settings _settings;
        private char[,] _currentChar;
        private ConsoleColor[,] _currentColor;
        private char[,] _nextChar;
        private ConsoleColor[,] _nextColor;

        private ConsoleColor _bgColor;
        private int _dirtyMinX, _dirtyMinY, _dirtyMaxX, _dirtyMaxY; //for dirty buffer
        public int Width { get; private set; }
        public int Height { get; private set; }

        #region Kernel32
        //all configs for DirectWrite

        //imports
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern nint GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleOutputCP(uint codePage);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool WriteConsoleOutput(
            nint hConsoleOutput,
            CHAR_INFO[,] lpBuffer,
            COORD dwBufferSize,
            COORD dwBufferCoord,
            ref SMALL_RECT lpWriteRegion
            );

        //structs
        [StructLayout(LayoutKind.Sequential)]
        private struct CHAR_INFO
        {
            public char UnicodeChar;
            public short Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SMALL_RECT
        {
            public short Left;
            public short Top;
            public short Right;
            public short Bottom;
        }

        //vars
        private const int STD_OUTPUT_HANDLE = -11;
        private static readonly nint ConsoleOutputHandle = GetStdHandle(STD_OUTPUT_HANDLE);
        private CHAR_INFO[,] _fastBuffer;

        #endregion
        public ScreenBuffer(Settings settings)
        {
            _settings = settings;

            Width = Console.WindowWidth;
            Height = Console.WindowHeight;

            _bgColor = settings.ConsoleSettings.BackgroundColor;
            _currentChar = new char[Height, Width];
            _currentColor = new ConsoleColor[Height, Width];

            _nextChar = new char[Height, Width];
            _nextColor = new ConsoleColor[Height, Width];

            if (_settings.RendererSettings.RendererMode == RenderMode.DirectWrite)
                _fastBuffer = new CHAR_INFO[Height, Width];

            Clear();
            Console.Clear();

            //force full redraw on first frame
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    _currentChar[y, x] = '\0';

            //for dirty buffering
            _dirtyMinX = int.MaxValue;
            _dirtyMinY = int.MaxValue;
            _dirtyMaxX = int.MinValue;
            _dirtyMaxY = int.MinValue;
        }

        public void Clear()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    _nextChar[y, x] = ' ';
                    _nextColor[y, x] = _bgColor;
                }
        }


        public void SetPixel(int x, int y, char c, ConsoleColor color = ConsoleColor.White)
        {
            //silently ignore if out of bounds
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            _nextChar[y, x] = c;
            _nextColor[y, x] = color;

            if (_settings.RendererSettings.RendererMode != RenderMode.DirtyRect) return;

            if (x < _dirtyMinX) _dirtyMinX = x;
            if (x > _dirtyMaxX) _dirtyMaxX = x;
            if (y < _dirtyMinY) _dirtyMinY = y;
            if (y > _dirtyMaxY) _dirtyMaxY = y;
        }

        public void DrawString(int x, int y, string text, ConsoleColor color = ConsoleColor.White)
        {
            for (int i = 0; i < text.Length; i++)
                SetPixel(x + i, y, text[i], color);
        }

        //TODO: add some color options
        public void DrawStatusBar(string text, float volume)
        {
            DrawString(0, Height - 1, text, ConsoleColor.Gray);

            // Volume indicator
            int meterLen = (int)(volume * 20);
            if (meterLen > 30) meterLen = 30;
            for (int i = 5; i < 5 + meterLen; i++)
            {
                if (i < Width - 1 && i < text.Length)
                    SetPixel(i, Height - 1, text[i], ConsoleColor.Cyan);
            }
        }

        public void UpdateBackgroundColor(ConsoleColor newColor)
        {
            _bgColor = newColor;
            Console.BackgroundColor = _bgColor;
            Console.Clear();

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    _currentChar[y, x] = '\0';
        }

        public void Render()
        {
            //handle console resize
            if (Width != Console.WindowWidth || Height != Console.WindowHeight)
            {
                Width = Console.WindowWidth;
                Height = Console.WindowHeight;

                _currentChar = new char[Height, Width];
                _currentColor = new ConsoleColor[Height, Width];
                _nextChar = new char[Height, Width];
                _nextColor = new ConsoleColor[Height, Width];

                if (_settings.RendererSettings.RendererMode == RenderMode.DirectWrite)
                    _fastBuffer = new CHAR_INFO[Height, Width];

                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        _currentChar[y, x] = '\0'; //set to NUL char

                Console.BackgroundColor = _bgColor;
                Console.Clear();
                return;
            }

            switch(_settings.RendererSettings.RendererMode)
            {
                case RenderMode.DirectWrite:
                    RenderDirectWrite(); 
                    break;

                case RenderMode.RowBatched:
                    RenderRowBatched(); 
                    break;

                case RenderMode.DirtyRect:
                    if (_dirtyMinX <= _dirtyMaxX && _dirtyMinY <= _dirtyMaxY)
                        RenderDirtyRect();
                    else
                        RenderPerPixel();
                    break;

                case RenderMode.PerPixel:
                default:
                    RenderPerPixel();
                    break;
            }
        }

        public string GetRendererMode() => _settings.RendererSettings.RendererMode.ToString();
        public void CycleRenderMode()
        {
            _settings.RendererSettings.RendererMode = Utility.CycleNextEnum(_settings.RendererSettings.RendererMode);
            ResetRenderState();
        }

        private void ResetRenderState()
        {
            //force full redraw
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    _currentChar[y, x] = '\0';

            //reset dirty rectangle
            _dirtyMinX = int.MaxValue;
            _dirtyMinY = int.MaxValue;
            _dirtyMaxX = int.MinValue;
            _dirtyMaxY = int.MinValue;

            //re-alloc fast buffer if switching to Fast mode
            if (_settings.RendererSettings.RendererMode == RenderMode.DirectWrite && _fastBuffer == null)
                _fastBuffer = new CHAR_INFO[Height, Width];
        }

        #region RendererModes
        private void RenderPerPixel()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (_nextChar[y, x] != _currentChar[y, x] || _nextColor[y, x] != _currentColor[y, x])
                    {
                        if (y < Console.WindowHeight && x < Console.WindowWidth)
                        {
                            Console.SetCursorPosition(x, y);
                            Console.BackgroundColor = _bgColor;
                            Console.ForegroundColor = _nextColor[y, x];
                            Console.Write(_nextChar[y, x]);
                        }
                        _currentChar[y, x] = _nextChar[y, x];
                        _currentColor[y, x] = _nextColor[y, x];
                    }
                }
            }

            Console.SetCursorPosition(0, Math.Min(Height - 1, Console.WindowHeight - 1));
            Console.ResetColor();
        }

        private void RenderDirtyRect()
        {
            for (int y = _dirtyMinY; y <= _dirtyMaxY; y++)
            {
                for (int x = _dirtyMinX; x <= _dirtyMaxX; x++)
                {
                    if (_nextChar[y, x] != _currentChar[y, x] || _nextColor[y, x] != _currentColor[y, x])
                    {
                        if (y < Console.WindowHeight && x < Console.WindowWidth)
                        {
                            Console.SetCursorPosition(x, y);
                            Console.BackgroundColor = _bgColor;
                            Console.ForegroundColor = _nextColor[y, x];
                            Console.Write(_nextChar[y, x]);
                        }
                        _currentChar[y, x] = _nextChar[y, x];
                        _currentColor[y, x] = _nextColor[y, x];
                    }
                }
            }

            _dirtyMinX = int.MaxValue;
            _dirtyMinY = int.MaxValue;
            _dirtyMaxX = int.MinValue;
            _dirtyMaxY = int.MinValue;

            Console.SetCursorPosition(0, Math.Min(Height - 1, Console.WindowHeight - 1));
            Console.ResetColor();
        }

        private void RenderRowBatched()
        {
            ConsoleColor fgColor = _settings.RendererSettings.RowBatchColor;
            StringBuilder sb = new StringBuilder();
            bool anyRowChanged = false;

            for (int y = 0; y < Height; y++)
            {
                bool rowChanged = false;
                sb.Clear();

                for (int x = 0; x < Width; x++)
                {
                    if (_nextChar[y, x] != _currentChar[y, x] || _nextColor[y, x] != _currentColor[y, x])
                    {
                        rowChanged = true;
                        anyRowChanged = true;
                    }
                    _currentChar[y, x] = _nextChar[y, x];
                    _currentColor[y, x] = _nextColor[y, x];
                    sb.Append(_nextChar[y, x]);
                }

                if (rowChanged)
                {
                    Console.SetCursorPosition(0, y);
                    Console.BackgroundColor = _bgColor;
                    Console.ForegroundColor = fgColor;
                    Console.Write(sb.ToString());
                }
            }

            if (anyRowChanged)
            {
                Console.SetCursorPosition(0, Math.Min(Height - 1, Console.WindowHeight - 1));
                Console.ResetColor();
            }
        }

        private void RenderDirectWrite()
        {
            bool anyChanged = false;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (_nextChar[y, x] != _currentChar[y, x] || _nextColor[y, x] != _currentColor[y, x])
                    {
                        anyChanged = true;
                        _fastBuffer[y, x].UnicodeChar = _nextChar[y, x];
                        _fastBuffer[y, x].Attributes = (short)((int)_nextColor[y, x] | (int)_bgColor << 4);

                        _currentChar[y, x] = _nextChar[y, x];
                        _currentColor[y, x] = _nextColor[y, x];
                    }
                }
            }

            if (!anyChanged) return;

            COORD bufferSize = new COORD { X = (short)Width, Y = (short)Height };
            COORD bufferCoord = new COORD { X = 0, Y = 0 };
            SMALL_RECT writeRegion = new SMALL_RECT
            {
                Left = 0,
                Top = 0,
                Right = (short)(Width - 1),
                Bottom = (short)(Height - 1)
            };

            WriteConsoleOutput(ConsoleOutputHandle, _fastBuffer, bufferSize, bufferCoord, ref writeRegion);
        }
        #endregion
    }
}
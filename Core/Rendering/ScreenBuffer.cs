using System.Runtime.InteropServices;
using System.Text;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Config.Settings.General;

#nullable disable warnings
namespace TERMINAL_FREQUENCY.Core.Rendering
{
    /// <summary>
    /// Double-buffered console rendering engine that minimizes flicker by only
    /// writing pixels that changed between frames. Supports four render modes:
    /// <see cref="RenderMode.PerPixel"/> (checks every cell),
    /// <see cref="RenderMode.DirtyRect"/> (only changed regions),
    /// <see cref="RenderMode.RowBatched"/> (entire rows as strings, one color),
    /// and <see cref="RenderMode.DirectWrite"/> (writes directly to console buffer via Win32).
    /// </summary>
    public class ScreenBuffer
    {
        private Settings _settings;
        private char[,] _currentChar;
        private ConsoleColor[,] _currentColor;
        private char[,] _nextChar;
        private ConsoleColor[,] _nextColor;

        private ConsoleColor _bgColor;
        private int _dirtyMinX, _dirtyMinY, _dirtyMaxX, _dirtyMaxY; //for dirty buffer

        /// <summary>The width of the console buffer in characters.</summary>
        public int Width { get; private set; }

        /// <summary>The height of the console buffer in characters.</summary>
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

        /// <summary>
        /// Initializes the screen buffer with the current console dimensions and configurable background color.
        /// Pre-allocates the fast buffer if <see cref="RenderMode.DirectWrite"/> is selected.
        /// Forces a full redraw on the first frame by marking all current pixels as null characters.
        /// </summary>
        /// <param name="settings">Application settings containing renderer and console configuration.</param>
        public ScreenBuffer(Settings settings)
        {
            _settings = settings;

            Width = Console.WindowWidth;
            Height = Console.WindowHeight;

            _bgColor = settings.Window.BackgroundColor;
            _currentChar = new char[Height, Width];
            _currentColor = new ConsoleColor[Height, Width];

            _nextChar = new char[Height, Width];
            _nextColor = new ConsoleColor[Height, Width];

            if (_settings.Renderer.RendererMode == RenderMode.DirectWrite)
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

        /// <summary>
        /// Clears the next frame buffer by setting every pixel to a space character
        /// with the current background color. Does not affect what is currently displayed.
        /// Call this at the start of each frame before drawing visualizations.
        /// </summary>
        public void Clear()
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    _nextChar[y, x] = ' ';
                    _nextColor[y, x] = _bgColor;
                }
        }

        /// <summary>
        /// Sets a single pixel in the next frame buffer. If the render mode is
        /// <see cref="RenderMode.DirtyRect"/>, also updates the dirty rectangle bounds.
        /// Out-of-bounds coordinates are silently ignored.
        /// </summary>
        /// <param name="x">The X coordinate (column) in characters.</param>
        /// <param name="y">The Y coordinate (row) in characters.</param>
        /// <param name="c">The character to display.</param>
        /// <param name="color">The foreground color for this pixel. Defaults to White.</param>
        public void SetPixel(int x, int y, char c, ConsoleColor color = ConsoleColor.White)
        {
            //silently ignore if out of bounds
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            _nextChar[y, x] = c;
            _nextColor[y, x] = color;

            if (_settings.Renderer.RendererMode != RenderMode.DirtyRect) return;

            if (x < _dirtyMinX) _dirtyMinX = x;
            if (x > _dirtyMaxX) _dirtyMaxX = x;
            if (y < _dirtyMinY) _dirtyMinY = y;
            if (y > _dirtyMaxY) _dirtyMaxY = y;
        }

        /// <summary>
        /// Draws a string of text by calling <see cref="SetPixel"/> for each character.
        /// </summary>
        /// <param name="x">Starting X coordinate.</param>
        /// <param name="y">Y coordinate for the entire string.</param>
        /// <param name="text">The text to draw.</param>
        /// <param name="color">Foreground color for all characters. Defaults to White.</param>
        public void DrawString(int x, int y, string text, ConsoleColor color = ConsoleColor.White)
        {
            for (int i = 0; i < text.Length; i++)
                SetPixel(x + i, y, text[i], color);
        }

        /// <summary>
        /// Draws a status bar at the bottom of the screen with a volume indicator.
        /// The indicator length is proportional to the provided volume value.
        /// </summary>
        /// <param name="text">The status text to display.</param>
        /// <param name="volume">Volume level used to calculate the indicator bar length.</param>
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

        /// <summary>
        /// Updates the background color and forces a full redraw on the next frame.
        /// Clears the actual console and marks all current pixels as invalid.
        /// </summary>
        /// <param name="newColor">The new background color to use.</param>
        public void UpdateBackgroundColor(ConsoleColor newColor)
        {
            _bgColor = newColor;
            Console.BackgroundColor = _bgColor;
            Console.Clear();

            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    _currentChar[y, x] = '\0';
        }

        /// <summary>
        /// Flushes the next frame buffer to the console. Handles window resize by
        /// re-allocating all buffers. Dispatches to the appropriate render method
        /// based on <see cref="RendererSettings.RendererMode"/>.
        /// </summary>
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

                if (_settings.Renderer.RendererMode == RenderMode.DirectWrite)
                    _fastBuffer = new CHAR_INFO[Height, Width];

                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        _currentChar[y, x] = '\0'; //set to NUL char

                Console.BackgroundColor = _bgColor;
                Console.Clear();
                return;
            }

            switch(_settings.Renderer.RendererMode)
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

        /// <summary>Returns the current render mode as a string for debug display.</summary>
        public string GetRendererMode() => _settings.Renderer.RendererMode.ToString();

        /// <summary>
        /// Cycles to the next render mode and resets the render state.
        /// Forces a full redraw on the next frame.
        /// </summary>
        public void CycleRenderMode()
        {
            _settings.Renderer.RendererMode = Utility.CycleNextEnum(_settings.Renderer.RendererMode);
            ResetRenderState();
        }

        /// <summary>
        /// Marks all current pixels as invalid, forcing a full redraw on the next render pass.
        /// Also resets the dirty rectangle and re-allocates the fast buffer if switching modes.
        /// </summary>
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
            if (_settings.Renderer.RendererMode == RenderMode.DirectWrite && _fastBuffer == null)
                _fastBuffer = new CHAR_INFO[Height, Width];
        }

        #region RendererModes
        /// <summary>
        /// Renders by checking every pixel for changes. Pixels that differ from the
        /// current buffer are written individually to the console with cursor positioning.
        /// Most reliable but slowest for large windows.
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

        /// <summary>
        /// Renders only the pixels within the tracked dirty rectangle bounds.
        /// Significantly faster than per-pixel when only a small area changes.
        /// Falls back to <see cref="RenderPerPixel"/> if no dirty region is tracked.
        /// Resets the dirty rectangle after rendering.
        /// </summary>
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

        /// <summary>
        /// Renders entire rows as single strings. Only rows with changes are written.
        /// Limited to a single foreground color per row from <see cref="RendererSettings.RowBatchColor"/>.
        /// Fast but monochrome per row.
        /// </summary>
        private void RenderRowBatched()
        {
            ConsoleColor fgColor = _settings.Renderer.RowBatchColor;
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

        /// <summary>
        /// Renders the entire console buffer in a single Win32 call using
        /// <see cref="WriteConsoleOutput"/>. The fastest render mode.
        /// Packs character and color attributes into <see cref="CHAR_INFO"/> structures
        /// and writes the whole buffer at once. No cursor movement overhead.
        /// </summary>
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
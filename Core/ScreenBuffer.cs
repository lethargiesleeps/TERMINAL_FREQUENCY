using System;

namespace TERMINAL_FREQUENCY.Core
{
    public class ScreenBuffer
    {
        private char[,] currentChar;
        private ConsoleColor[,] currentColor;
        private char[,] nextChar;
        private ConsoleColor[,] nextColor;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public ScreenBuffer()
        {
            Width = Console.WindowWidth;
            Height = Console.WindowHeight;

            currentChar = new char[Height, Width];
            currentColor = new ConsoleColor[Height, Width];
            nextChar = new char[Height, Width];
            nextColor = new ConsoleColor[Height, Width];

            Clear();

            // Force full redraw on first frame
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    currentChar[y, x] = '\0';
        }

        public void Clear()
        {
            ConsoleColor bgColor = Config.Config.DARK_MODE ? ConsoleColor.Black : ConsoleColor.White;
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    nextChar[y, x] = ' ';
                    nextColor[y, x] = bgColor;
                }
        }

        public void SetPixel(int x, int y, char c, ConsoleColor color = ConsoleColor.White)
        {
            //silently ignore if out of bounds
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            nextChar[y, x] = c;
            nextColor[y, x] = color;
        }

        public void DrawString(int x, int y, string text, ConsoleColor color = ConsoleColor.White)
        {
            for (int i = 0; i < text.Length; i++)
                SetPixel(x + i, y, text[i], color);
        }

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

        public void Render()
        {
            ConsoleColor bgColor = Config.Config.DARK_MODE ? ConsoleColor.Black : ConsoleColor.White;

            //handle console resize
            if (Width != Console.WindowWidth || Height != Console.WindowHeight)
            {
                Width = Console.WindowWidth;
                Height = Console.WindowHeight;

                currentChar = new char[Height, Width];
                currentColor = new ConsoleColor[Height, Width];
                nextChar = new char[Height, Width];
                nextColor = new ConsoleColor[Height, Width];

                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        currentChar[y, x] = '\0';
                Console.BackgroundColor = bgColor;
                Console.Clear();
                return;
            }


            //render only changed pixels
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (nextChar[y, x] != currentChar[y, x] || nextColor[y, x] != currentColor[y, x])
                    {
                        if (y < Console.WindowHeight && x < Console.WindowWidth)
                        {
                            Console.SetCursorPosition(x, y);
                            Console.BackgroundColor = bgColor;
                            Console.ForegroundColor = nextColor[y, x];
                            Console.Write(nextChar[y, x]);
                        }
                        currentChar[y, x] = nextChar[y, x];
                        currentColor[y, x] = nextColor[y, x];
                    }
                }
            }

            Console.SetCursorPosition(0, Math.Min(Height - 1, Console.WindowHeight - 1));
            Console.ResetColor();
        }
    }
}
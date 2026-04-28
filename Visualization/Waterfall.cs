using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Config;
using TERMINAL_FREQUENCY.Core;

namespace TERMINAL_FREQUENCY.Visualization
{
    public class Waterfall : IVisualization
    {
        private string name = "WATERFALL";
        private int modeIndex = 1;
        private List<WaterfallStream> streams = new List<WaterfallStream>();
        private readonly object streamLock = new object();
        private int clockwiseIndex = 0;
        private bool topBottomToggle = true; // true = top, false = bottom
        private bool leftRightToggle = true; // true = left, false = right

        private static readonly VisualizationOrigin[] ClockwiseOrder =
            { VisualizationOrigin.Top, VisualizationOrigin.Right, VisualizationOrigin.Bottom, VisualizationOrigin.Left };

        private static readonly VisualizationOrigin[] AntiClockwiseOrder =
            { VisualizationOrigin.Top, VisualizationOrigin.Left, VisualizationOrigin.Bottom, VisualizationOrigin.Right };

        string IVisualization.Name => name;
        int IVisualization.ModeIndex => modeIndex;

        public Waterfall()
        {
            clockwiseIndex = Array.IndexOf(ClockwiseOrder, Config.Config.WATERFALL_ORIGIN);
            if (clockwiseIndex < 0) clockwiseIndex = 0;
        }

        public void Update(float volume)
        {
            lock (streamLock)
            {
                for (int i = streams.Count - 1; i >= 0; i--)
                {
                    streams[i].Update();
                    if (!streams[i].IsAlive)
                        streams.RemoveAt(i);
                }
            }
        }

        public void OnSpike(float intensity)
        {
            if (Config.Config.WATERFALL_ONLY_SPAWN_ON_THRESHOLD && intensity < Config.Config.WATERFALL_TRIGGER_THRESHOLD) return;

            lock (streamLock)
            {
                if (Config.Config.WATERFALL_MODE == WaterfallMode.All)
                {
                    VisualizationOrigin[] allOrigins =
                    {
                        VisualizationOrigin.Top,
                        VisualizationOrigin.Bottom,
                        VisualizationOrigin.Left,
                        VisualizationOrigin.Right
                    };

                    foreach (VisualizationOrigin o in allOrigins)
                    {
                        while (streams.Count >= Config.Config.WATERFALL_MAX_STREAMS)
                            streams.RemoveAt(0);

                        bool isReversed = Config.Config.WATERFALL_REVERSE_MODE;
                        streams.Add(new WaterfallStream(intensity, o, isReversed));
                    }
                }
                else
                {
                    if (streams.Count >= Config.Config.WATERFALL_MAX_STREAMS)
                        streams.RemoveAt(0);

                    VisualizationOrigin origin = GetNextOrigin();
                    streams.Add(new WaterfallStream(intensity, origin, Config.Config.WATERFALL_REVERSE_MODE));
                }
            }

            // Update all streams
            lock (streamLock)
            {
                for (int i = streams.Count - 1; i >= 0; i--)
                {
                    streams[i].Update();
                    if (!streams[i].IsAlive)
                        streams.RemoveAt(i);
                }
            }
        }

        public void Draw(ScreenBuffer buffer)
        {
            List<WaterfallStream> streamsCopy;
            lock(streamLock)
            {
                streamsCopy = new List<WaterfallStream>(streams);
            }

            foreach (WaterfallStream stream in streamsCopy)
                DrawStream(buffer, stream);
        }

        private VisualizationOrigin GetNextOrigin()
        {
            switch (Config.Config.WATERFALL_MODE)
            {
                case WaterfallMode.Normal:
                    return Config.Config.WATERFALL_ORIGIN;

                case WaterfallMode.Clockwise:
                    int startIdx = Array.IndexOf(ClockwiseOrder, Config.Config.WATERFALL_ORIGIN);
                    VisualizationOrigin next = ClockwiseOrder[(startIdx + clockwiseIndex) % 4];
                    clockwiseIndex = (clockwiseIndex + 1) % 4;
                    return next;

                case WaterfallMode.AntiClockwise:
                    startIdx = Array.IndexOf(AntiClockwiseOrder, Config.Config.WATERFALL_ORIGIN);
                    next = AntiClockwiseOrder[(startIdx + clockwiseIndex) % 4];
                    clockwiseIndex = (clockwiseIndex + 1) % 4;
                    return next;

                case WaterfallMode.TopBottom:
                    topBottomToggle = !topBottomToggle;
                    return topBottomToggle ? VisualizationOrigin.Top : VisualizationOrigin.Bottom;

                case WaterfallMode.LeftRight:
                    leftRightToggle = !leftRightToggle;
                    return leftRightToggle ? VisualizationOrigin.Left : VisualizationOrigin.Right;
                case WaterfallMode.All:
                    return (VisualizationOrigin)(new Random().Next(4)); // 0=Top, 1=Bottom, 2=Left, 3=Right

                default:
                    return Config.Config.WATERFALL_ORIGIN;
            }
        }
        private void DrawStream(ScreenBuffer buffer, WaterfallStream stream)
        {
            switch(stream.Origin)
            {
                case VisualizationOrigin.Top:
                case VisualizationOrigin.Center:
                    DrawVerticalStream(buffer, stream, fromTop: true);
                    break;
                case VisualizationOrigin.Bottom:
                    DrawVerticalStream(buffer, stream, fromTop: false);
                    break;
                case VisualizationOrigin.Left:
                    DrawHorizontalStream(buffer, stream, fromLeft: true);
                    break;
                case VisualizationOrigin.Right:
                    DrawHorizontalStream(buffer, stream, fromLeft: false);
                    break;
            }
        }
        private void DrawVerticalStream(ScreenBuffer buffer, WaterfallStream stream, bool fromTop)
        {
            int consoleHeight = buffer.Height;
            int consoleWidth = buffer.Width;

            //calcY
            int streamY;

            if(stream.IsReversed)
            {
                //start from middle, move toward origin edge
                int centerY = consoleHeight / 2;
                if (fromTop)
                    streamY = centerY - (int)(stream.Progress * centerY);
                else
                    streamY = centerY + (int)(stream.Progress * (consoleHeight - centerY));
            }
            else
            {
                if (fromTop)
                    streamY = (int)(stream.Progress * consoleHeight);
                else
                    streamY = consoleHeight - 1 - (int)(stream.Progress * consoleHeight);
            }

            //calculate width
            int halfWidth = (int)(stream.GetWidth(consoleWidth) / 2);
            int centerX = consoleWidth / 2;

            //draw
            int startX = Math.Max(0, centerX - halfWidth);
            int endX = Math.Min(consoleWidth - 1, centerX + halfWidth);

            ConsoleColor color = stream.GetColor();

            for (int x = startX; x <= endX; x++)
            {
                int positionInStream = x - startX;
                int totalPositions = endX - startX + 1;

                char c = stream.GetCharacter(positionInStream, totalPositions);

                if (c != ' ')
                {
                    buffer.SetPixel(x, streamY, c, color);
                }
            }

            //curve
            int curveOffset = (int)(halfWidth * Config.Config.WATERFALL_CURVE_INTENSITY_VERITCAL);
            if (curveOffset > 0 && streamY > 0 && streamY < consoleHeight - 1)
            {
                int curveY = fromTop ? streamY - 1 : streamY + 1;
                if (curveY >= 0 && curveY < consoleHeight)
                    for (int x = centerX - curveOffset; x <= centerX + curveOffset; x++)
                        if (x >= 0 && x < consoleWidth)
                            buffer.SetPixel(x, curveY, Config.Config.WATERFALL_CURVE_CHAR, color);
            }

            if (Config.Config.DEBUG_MODE)
            {
                string debug = $"P:{stream.Progress:F2} L:{stream.Life:F2}";
                buffer.DrawString(0, 0, debug, ConsoleColor.Yellow);
            }
        }

        private void DrawHorizontalStream(ScreenBuffer buffer, WaterfallStream stream, bool fromLeft)
        {
            int consoleHeight = buffer.Height;
            int consoleWidth = buffer.Width;

            //calcX
            int streamX;
            if (stream.IsReversed)
            {
                int centerX = consoleWidth / 2;
                if (fromLeft)
                    streamX = centerX - (int)(stream.Progress * centerX);
                else
                    streamX = centerX + (int)(stream.Progress * (consoleWidth - centerX));
            }
            else
            {
                if (fromLeft)
                    streamX = (int)(stream.Progress * consoleWidth);
                else
                    streamX = consoleWidth - 1 - (int)(stream.Progress * consoleWidth);
            }

            //calc width
            int halfHeight = (int)(stream.GetWidth(consoleHeight) / 2);
            int centerY = consoleHeight / 2;

            //draw
            int startY = Math.Max(0, centerY - halfHeight);
            int endY = Math.Min(consoleHeight - 1, centerY + halfHeight);

            ConsoleColor color = stream.GetColor();

            for (int y = startY; y <= endY; y++)
            {
                int positionInStream = y - startY;
                int totalPositions = endY - startY + 1;

                char c = stream.GetCharacter(positionInStream, totalPositions);

                if (c != ' ')
                {
                    if (c == Config.Config.WATERFALL_VERTICAL_CHARS[0]) c = Config.Config.WATERFALL_HORIZONTAL_CHARS[0];
                    else if (c == Config.Config.WATERFALL_VERTICAL_CHARS[1]) c = Config.Config.WATERFALL_HORIZONTAL_CHARS[1];
                    else if (c == Config.Config.WATERFALL_VERTICAL_CHARS[2]) c = Config.Config.WATERFALL_HORIZONTAL_CHARS[2];

                    buffer.SetPixel(streamX, y, c, color);
                }
            }

            //curvature
            int curveOffset = (int)(halfHeight * Config.Config.WATERFALL_CURVE_INTENSITY_HORIZONTAL);
            if (curveOffset > 0 && streamX > 0 && streamX < consoleWidth - 1)
            {
                int curveX = fromLeft ? streamX - 1 : streamX + 1;
                if (curveX >= 0 && curveX < consoleWidth)
                    for (int y = centerY - curveOffset; y <= centerY + curveOffset; y++)
                        if (y >= 0 && y < consoleHeight)
                            buffer.SetPixel(curveX, y, Config.Config.WATERFALL_CURVE_CHAR, color);
            }

            if (Config.Config.DEBUG_MODE)
            {
                string debug = $"P:{stream.Progress:F2} L:{stream.Life:F2}";
                buffer.DrawString(0, 0, debug, ConsoleColor.Yellow);
            }
        }
    }
}

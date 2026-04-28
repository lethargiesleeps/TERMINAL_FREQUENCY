using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Config;
using TERMINAL_FREQUENCY.Core;

namespace TERMINAL_FREQUENCY.Visualization
{
    public class WaterfallStream
    {
        public float Progress; //0 is origin, 1 is furthest edge
        public float Life; //1 is full life, 0 is dead
        public float Intensity;
        public VisualizationOrigin Origin;
        private ConsoleColor streamColor;
        private static ConsoleColor lastColor = ConsoleColor.White;
        private static readonly Random rng = new Random();

        private static readonly ConsoleColor[] RainbowColors =
        {
            ConsoleColor.Red, ConsoleColor.DarkRed,
            ConsoleColor.Yellow, ConsoleColor.DarkYellow,
            ConsoleColor.Green, ConsoleColor.DarkGreen,
            ConsoleColor.Cyan, ConsoleColor.DarkCyan,
            ConsoleColor.Blue, ConsoleColor.DarkBlue,
            ConsoleColor.Magenta, ConsoleColor.DarkMagenta
        };

        public WaterfallStream(float intensity, VisualizationOrigin origin)
        {
            Progress = 0;
            Life = 1.0f;
            Intensity = Math.Min(1.0f, intensity);
            Origin = origin == VisualizationOrigin.Center ? VisualizationOrigin.Top : origin;

            if (Config.Config.WATERFALL_RAINBOW_MODE)
            {
                ConsoleColor newColor;
                do
                {
                    newColor = RainbowColors[rng.Next(RainbowColors.Length)];
                } while (newColor == lastColor);

                streamColor = newColor;
                lastColor = newColor;
            }
        }

        public void Update()
        {
            Progress += Config.Config.WATERFALL_SPEED * 0.025f; //0.025f is speed scaling factor
            if(Progress > 1.0f) Progress = 1.0f;
            Life -= Config.Config.WATERFALL_FADE_RATE;
            if (Progress >= 1.0f)
                Life = 0;
        }

        public bool IsAlive => Life > 0 && Progress < 1.0f;

        public float GetWidth(float consoleSize)
        {
            float startWidth = consoleSize * Config.Config.WATERFALL_START_WIDTH_PERCENT;
            float endWidth = consoleSize * Config.Config.WATERFALL_END_WIDTH_PERCENT;

            //linear interp
            return startWidth + (endWidth - startWidth) * Progress;
        }

        public char GetCharacter(int position, int totalPositions)
        {
            float positionRatio = (float)position / totalPositions;
            if (Progress < Config.Config.WATERFALL_MIDPOINT_CHANGE)
                return positionRatio < 0.3f || positionRatio > 0.7f ? Config.Config.WATERFALL_VERTICAL_CHARS[0] : ' ';
            else if (Progress < Config.Config.WATERFALL_ENDPOINT_CHANGE)
                return positionRatio < 0.2f || positionRatio > 0.8f ? Config.Config.WATERFALL_VERTICAL_CHARS[1] :
                       (position % 3 == 0 ? Config.Config.WATERFALL_VERTICAL_CHARS[2] : ' ');
            else
                return position % 4 == 0 ? Config.Config.WATERFALL_VERTICAL_CHARS[2] : ' ';
        }

        public ConsoleColor GetColor()
        {
            if (Config.Config.WATERFALL_RAINBOW_MODE)
            {
                if (Progress < Config.Config.WATERFALL_RAINBOW_FADE_BRIGHT)
                    return ConsoleColor.White;
                else if (Progress < Config.Config.WATERFALL_RAINBOW_FADE_COLOR)
                    return streamColor;
                else if (Progress < Config.Config.WATERFALL_RAINBOW_FADE_DARK)
                    return Utility.DarkenColor(streamColor);
                else if (Progress < Config.Config.WATERFALL_RAINBOW_FADE_DARKGRAY)
                    return ConsoleColor.DarkGray;
                else
                    return ConsoleColor.Black;
            }
            else
            {
                if (Progress < Config.Config.WATERFALL_NORMAL_FADE_WHITE)
                    return ConsoleColor.White;
                else if (Progress < Config.Config.WATERFALL_NORMAL_FADE_GRAY)
                    return ConsoleColor.Gray;
                else if (Progress < Config.Config.WATERFALL_NORMAL_FADE_DARKGRAY)
                    return ConsoleColor.DarkGray;
                else
                    return ConsoleColor.Black;
            }
        }
    }
}

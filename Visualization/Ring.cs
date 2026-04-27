using System;
using System.Diagnostics;
using TERMINAL_FREQUENCY.Core;

namespace TERMINAL_FREQUENCY.Visualization
{
    public class Ring
    {
        public float Radius { get; set; }
        public float Life { get; set; }
        private Random random;
        public Ring()
        {
            Radius = Config.Config.RING_RADIUS;
            Life = Config.Config.RING_LIFETIME;
            random = new Random();
        }

        public void Update(float speed = 0.7f, float fadeRate = 0.02f)
        {
            Radius += speed;
            Life -= fadeRate;
        }

        public bool IsAlive => Life > 0 && Radius <= Config.Config.RING_RADIUS_MAX;

        public ConsoleColor GetColor()
        {
            //all floats are between 0 and 1 once normalized
            float normalizedLife = Config.Config.RING_LIFETIME == 1.0f ? Life
                : Life / Config.Config.RING_LIFETIME;
            
            switch (Config.Config.RING_COLOR_MODE)
            {
                case Config.ColorMode.Light:
                    if (normalizedLife > 0.6f) return ConsoleColor.White;
                    if (normalizedLife > 0.3f) return ConsoleColor.Gray;
                    return ConsoleColor.DarkGray;
                case Config.ColorMode.Dark: //use if console bg colour not black
                    if (normalizedLife > 0.6f) return ConsoleColor.Black;
                    if (normalizedLife > 0.3f) return ConsoleColor.DarkGray;
                    return ConsoleColor.Gray;
                case Config.ColorMode.Red:
                    if (normalizedLife > 0.6f) return ConsoleColor.Red;
                    if (normalizedLife > 0.3f) return ConsoleColor.DarkRed;
                    return ConsoleColor.DarkGray;
                case Config.ColorMode.Green:
                    if (normalizedLife > 0.6f) return ConsoleColor.Green;
                    if (normalizedLife > 0.3f) return ConsoleColor.DarkGreen;
                    return ConsoleColor.DarkGray;
                case Config.ColorMode.Blue:
                    if (normalizedLife > 0.9f) return ConsoleColor.Cyan;
                    if (normalizedLife > 0.6f) return ConsoleColor.Blue;
                    if (normalizedLife > 0.3f) return ConsoleColor.DarkCyan;
                    return ConsoleColor.DarkBlue;
                case Config.ColorMode.Yellow:
                    if (normalizedLife > 0.6f) return ConsoleColor.White;
                    if (normalizedLife > 0.3f) return ConsoleColor.Yellow;
                    return ConsoleColor.DarkYellow;
                case Config.ColorMode.RainbowLight:
                    if (normalizedLife > 0.83f) return ConsoleColor.Red;
                    if (normalizedLife > 0.66f) return ConsoleColor.Yellow;
                    if (normalizedLife > 0.5f) return ConsoleColor.Green;
                    if (normalizedLife > 0.33f) return ConsoleColor.Cyan;
                    if (normalizedLife > 0.16f) return ConsoleColor.Blue;
                    return ConsoleColor.Magenta;
                case Config.ColorMode.RainbowDark:
                    if (normalizedLife > 0.83f) return ConsoleColor.DarkRed;
                    if (normalizedLife > 0.66f) return ConsoleColor.DarkYellow;
                    if (normalizedLife > 0.5f) return ConsoleColor.DarkGreen;
                    if (normalizedLife > 0.33f) return ConsoleColor.DarkCyan;
                    if (normalizedLife > 0.16f) return ConsoleColor.DarkBlue;
                    return ConsoleColor.Magenta;
                default:
                    return ConsoleColor.White;

            }
            
        }

        public char GetChar(int segmentIndex)
        {
            if (Config.Config.USE_RING_CHAR_RANDOMIZER)
            {
                string charSet = Config.Config.RING_CHAR_RANDOMIZER_CHARSET;
                int randomIndex = random.Next(0, charSet.Length);
                return charSet[randomIndex];
            }
            else
            {
                if (segmentIndex % 6 == 0) return Config.Config.RING_CHARACTERS[0];
                if (segmentIndex % 3 == 0) return Config.Config.RING_CHARACTERS[1];
                return Config.Config.RING_CHARACTERS[2];
            }

        }
    }
}

using System;
using System.Diagnostics;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Core;

namespace TERMINAL_FREQUENCY.Visualization.Rings
{
    public class Ring
    {
        public float Radius { get; set; }
        public float Life { get; set; } //1 is full life, 0 is dead

        private readonly Random _rnd;
        private Settings _settings;

        public Ring(Settings settings)
        {
            _settings = settings;
            Radius = _settings.RingsSettings.ReverseMode ? _settings.RingsSettings.RadiusMax : _settings.RingsSettings.Radius;
            Life = _settings.RingsSettings.Lifetime;
            _rnd = new Random();
        }

        public void Update(float speed = 0.7f, float fadeRate = 0.02f)
        {
            Radius += _settings.RingsSettings.ReverseMode ? -speed : speed;
            Life -= fadeRate;
        }

        public bool IsAlive => _settings.RingsSettings.ReverseMode
        ? Life > 0 && Radius >= _settings.RingsSettings.RadiusMin //alive while above min
        : Life > 0 && Radius <= _settings.RingsSettings.RadiusMax; //alive while below max

        public ConsoleColor GetColor()
        {
            //all floats are between 0 and 1 once normalized
            float normalizedLife = _settings.RingsSettings.Lifetime == 1.0f ? Life
                : Life / _settings.RingsSettings.Lifetime;


            switch (_settings.RingsSettings.ColorMode)
            {
                case RingColorMode.Light:
                    if (normalizedLife > 0.6f || _settings.RingsSettings.SolidColor) return ConsoleColor.White;
                    if (normalizedLife > 0.3f) return ConsoleColor.Gray;
                    return ConsoleColor.DarkGray;
                case RingColorMode.Dark: //use if console bg colour not black
                    if (normalizedLife > 0.6f || _settings.RingsSettings.SolidColor) return ConsoleColor.Black;
                    if (normalizedLife > 0.3f) return ConsoleColor.DarkGray;
                    return ConsoleColor.Gray;
                case RingColorMode.Red:
                    if (_settings.RingsSettings.SolidColor) return ConsoleColor.Red;
                    if (normalizedLife > 0.9f) return ConsoleColor.Magenta;
                    if (normalizedLife > 0.6f) return ConsoleColor.Red;
                    if (normalizedLife > 0.3f) return ConsoleColor.DarkMagenta;
                    return ConsoleColor.DarkRed;
                case RingColorMode.Green:
                    if (normalizedLife > 0.6f || _settings.RingsSettings.SolidColor) return ConsoleColor.Green;
                    if (normalizedLife > 0.3f) return ConsoleColor.DarkGreen;
                    return ConsoleColor.DarkGray;
                case RingColorMode.Blue:
                    if (_settings.RingsSettings.SolidColor) return ConsoleColor.Blue;
                    if (normalizedLife > 0.9f) return ConsoleColor.Cyan;
                    if (normalizedLife > 0.6f) return ConsoleColor.Blue;
                    if (normalizedLife > 0.3f) return ConsoleColor.DarkCyan;
                    return ConsoleColor.DarkBlue;
                case RingColorMode.Yellow:
                    if (_settings.RingsSettings.SolidColor) return ConsoleColor.Yellow;
                    if (normalizedLife > 0.6f) return ConsoleColor.White;
                    if (normalizedLife > 0.3f) return ConsoleColor.Yellow;
                    return ConsoleColor.DarkYellow;
                case RingColorMode.RainbowLight:
                    if (normalizedLife > 0.83f) return ConsoleColor.Red;
                    if (normalizedLife > 0.66f) return ConsoleColor.Yellow;
                    if (normalizedLife > 0.5f) return ConsoleColor.Green;
                    if (normalizedLife > 0.33f) return ConsoleColor.Cyan;
                    if (normalizedLife > 0.16f) return ConsoleColor.Blue;
                    return ConsoleColor.Magenta;
                case RingColorMode.RainbowDark:
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
            if (_settings.RingsSettings.CharRandomizer)
            {
                string charSet = _settings.RingsSettings.CharRandomizerCharset;
                int randomIndex = _rnd.Next(0, charSet.Length);
                return charSet[randomIndex];
            }
            else
            {
                char[] chars = _settings.RingsSettings.Characters;
                if (chars == null || chars.Length == 0)
                    return 'O';

                int index = segmentIndex % chars.Length;
                return chars[index];
            }
        }
    }
}

using TERMINAL_FREQUENCY.Config.Settings;

namespace TERMINAL_FREQUENCY.Visualization.Rings
{
    /// <summary>
    /// Represents a single expanding or shrinking ring with configurable radius, color, and character.
    /// Supports reverse mode (shrink inward), solid color mode, rainbow color cycling, and
    /// character randomization. Each ring tracks its own lifecycle from spawn to death.
    /// </summary>
    public class Ring
    {
        /// <summary>Current radius of the ring in character units.</summary>
        public float Radius { get; set; }

        /// <summary>Remaining lifespan. 1 = full life, 0 = dead.</summary>
        public float Life { get; set; }

        private readonly Random _rnd;
        private Settings _settings;

        /// <summary>
        /// Creates a new ring. Starting radius depends on <see cref="RingsSettings.ReverseMode"/>:
        /// normal starts at <see cref="RingsSettings.Radius"/>, reverse starts at <see cref="RingsSettings.RadiusMax"/>.
        /// </summary>
        /// <param name="settings">The application settings containing ring configuration.</param>
        public Ring(Settings settings)
        {
            _settings = settings;
            Radius = _settings.RingsSettings.ReverseMode ? _settings.RingsSettings.RadiusMax : _settings.RingsSettings.Radius;
            Life = _settings.RingsSettings.Lifetime;
            _rnd = new Random();
        }

        /// <summary>
        /// Advances the ring's radius by speed (outward in normal mode, inward in reverse)
        /// and reduces life by fadeRate. Called each frame by <see cref="Rings.Update"/>.
        /// </summary>
        /// <param name="speed">How many character units to expand per frame.</param>
        /// <param name="fadeRate">Life subtracted per frame. Higher values make rings die faster.</param>
        public void Update(float speed = 0.7f, float fadeRate = 0.02f)
        {
            Radius += _settings.RingsSettings.ReverseMode ? -speed : speed;
            Life -= fadeRate;
        }

        /// <summary>
        /// Returns true if the ring is still visible. In normal mode, alive while below <see cref="RingsSettings.RadiusMax"/>.
        /// In reverse mode, alive while above <see cref="RingsSettings.RadiusMin"/>.
        /// </summary>
        public bool IsAlive => _settings.RingsSettings.ReverseMode
        ? Life > 0 && Radius >= _settings.RingsSettings.RadiusMin //alive while above min
        : Life > 0 && Radius <= _settings.RingsSettings.RadiusMax; //alive while below max

        /// <summary>
        /// Returns the display color for this ring based on its normalized remaining life.
        /// Supports multiple <see cref="RingColorMode"/> options including solid, gradient, and rainbow.
        /// When <see cref="RingsSettings.SolidColor"/> is enabled, some modes return a single uniform color.
        /// </summary>
        /// <returns>The <see cref="ConsoleColor"/> for this ring at its current lifecycle stage.</returns>
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

        /// <summary>
        /// Returns the character to display at a given segment of the ring.
        /// If <see cref="RingsSettings.CharRandomizer"/> is enabled, picks a random character
        /// from <see cref="RingsSettings.CharRandomizerCharset"/>. Otherwise cycles through
        /// <see cref="RingsSettings.Characters"/> based on segment index. Defaults to 'O' if no characters defined.
        /// </summary>
        /// <param name="segmentIndex">The segment position around the ring (0-based).</param>
        /// <returns>The character to draw at this segment.</returns>
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

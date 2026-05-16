using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Config.Settings.Visualizations;
using TERMINAL_FREQUENCY.Core;

namespace TERMINAL_FREQUENCY.Visualization.Waterfall
{
    /// <summary>
    /// Represents a single waterfall stream that flows across the console.
    /// Each stream has a progress from origin (0) to far edge (1), fading over time.
    /// </summary>
    public class WaterfallStream
    {
        private Settings _settings;
        private ConsoleColor _streamColor;
        private static ConsoleColor _lastColor = ConsoleColor.White;
        private static readonly Random _rnd = new Random();
        private static readonly ConsoleColor[] _rainbowColors =
{
            ConsoleColor.Red, ConsoleColor.DarkRed,
            ConsoleColor.Yellow, ConsoleColor.DarkYellow,
            ConsoleColor.Green, ConsoleColor.DarkGreen,
            ConsoleColor.Cyan, ConsoleColor.DarkCyan,
            ConsoleColor.Blue, ConsoleColor.DarkBlue,
            ConsoleColor.Magenta, ConsoleColor.DarkMagenta
        };

        /// <summary>How far the stream has traveled from its origin. 0 = origin, 1 = far edge.</summary>
        public float Progress;

        /// <summary>Remaining lifespan of the stream. 1 = full life, 0 = dead.</summary>
        public float Life;

        /// <summary>Intensity of the audio spike that created this stream, clamped to 1.0.</summary>
        public float Intensity;

        /// <summary>Whether this stream flows in reverse (center to edge instead of edge to center).</summary>
        public bool IsReversed;

        /// <summary>The screen edge this stream originates from. <see cref="VisualizationOrigin"/> defaults to Top.</summary>
        public VisualizationOrigin Origin;

        /// <summary>Pixel offset for thickness. Multiple streams with different offsets create a thicker visual. Range: -2 to 2.</summary>
        public int ThicknessOffset { get; set; }

        /// <summary>
        /// Creates a new waterfall stream with the given intensity and origin.
        /// If <see cref="WaterfallSettings.RainbowMode"/> is enabled, assigns a random color from the rainbow palette
        /// that differs from the previous stream's color.
        /// </summary>
        public WaterfallStream(Settings settings, float intensity, VisualizationOrigin origin, bool isReversed = false)
        {
            _settings = settings;
            Progress = 0;
            Life = 1.0f;
            Intensity = Math.Min(1.0f, intensity);
            Origin = origin == VisualizationOrigin.Center ? VisualizationOrigin.Top : origin;
            IsReversed = isReversed;

            if (_settings.WaterfallSettings.RainbowMode)
            {
                ConsoleColor newColor;
                do
                {
                    newColor = _rainbowColors[_rnd.Next(_rainbowColors.Length)];
                } while (newColor == _lastColor);

                _streamColor = newColor;
                _lastColor = newColor;
            }
        }

        /// <summary>
        /// Advances the stream's <see cref="Progress"/> by <see cref="WaterfallSettings.Speed"/> and reduces
        /// <see cref="Life"/> by <see cref="WaterfallSettings.FadeRate"/>. When progress reaches 1.0, life is set to 0.
        /// </summary>
        public void Update()
        {
            Progress += _settings.WaterfallSettings.Speed * 0.025f; //0.025f is speed scaling factor
            if(Progress > 1.0f) Progress = 1.0f;
            Life -= _settings.WaterfallSettings.FadeRate;
            if (Progress >= 1.0f)
                Life = 0;
        }

        /// <summary>Returns true if the stream still has life remaining and hasn't reached the far edge.</summary>
        public bool IsAlive => Life > 0 && Progress < 1.0f;

        /// <summary>
        /// Calculates the current display width of the stream based on its progress.
        /// Linearly interpolates between <see cref="WaterfallSettings.StartWidthPercent"/> and
        /// <see cref="WaterfallSettings.EndWidthPercent"/> of the console dimension.
        /// </summary>
        /// <param name="consoleSize">The width or height of the console, depending on flow direction.</param>
        public float GetWidth(float consoleSize)
        {
            float startWidth = consoleSize * _settings.WaterfallSettings.StartWidthPercent;
            float endWidth = consoleSize * _settings.WaterfallSettings.EndWidthPercent;

            //linear interp
            return startWidth + (endWidth - startWidth) * Progress;
        }

        /// <summary>
        /// Determines which character to draw at a given position within the stream.
        /// Characters change based on <see cref="Progress"/>: solid edges early, breaking up in the middle,
        /// and scattered near the end. Uses <see cref="WaterfallSettings.VerticalChars"/> for the character set.
        /// </summary>
        /// <param name="position">Position within the stream's current row/column.</param>
        /// <param name="totalPositions">Total width/height of the stream at this point.</param>
        /// <returns>The character to draw, or space if this position should be empty.</returns>
        public char GetCharacter(int position, int totalPositions)
        {
            float positionRatio = (float)position / totalPositions;
            if (Progress < _settings.WaterfallSettings.MidpointChange)
                return positionRatio < 0.3f || positionRatio > 0.7f ? _settings.WaterfallSettings.VerticalChars[0] : ' ';
            else if (Progress < _settings.WaterfallSettings.EndpointChange)
                return positionRatio < 0.2f || positionRatio > 0.8f ? _settings.WaterfallSettings.VerticalChars[1] :
                       position % 3 == 0 ? _settings.WaterfallSettings.VerticalChars[2] : ' ';
            else
                return position % 4 == 0 ? _settings.WaterfallSettings.VerticalChars[2] : ' ';
        }

        /// <summary>
        /// Returns the display color for the stream based on its <see cref="Progress"/>.
        /// In rainbow mode, transitions from white to the assigned <see cref="_streamColor"/> to dark to black.
        /// In normal mode, transitions from white to <see cref="WaterfallSettings.Color"/> to dark to black.
        /// Uses <see cref="WaterfallSettings"/> fade thresholds to control transition points.
        /// </summary>
        public ConsoleColor GetColor()
        {
            if (_settings.WaterfallSettings.RainbowMode)
            {
                if (Progress < _settings.WaterfallSettings.RainbowFadeBright)
                    return ConsoleColor.White;
                else if (Progress < _settings.WaterfallSettings.RainbowFadeColor)
                    return _streamColor;
                else if (Progress < _settings.WaterfallSettings.RainbowFadeDark)
                    return Utility.DarkenColor(_streamColor);
                else if (Progress < _settings.WaterfallSettings.RainbowFadeDarkGray)
                    return ConsoleColor.DarkGray;
                else
                    return ConsoleColor.Black;
            }
            else
            {
                if (Progress < _settings.WaterfallSettings.NormalFadeWhite)
                    return ConsoleColor.White;
                else if (Progress < _settings.WaterfallSettings.NormalFadeGray)
                    return _settings.WaterfallSettings.Color;
                else if (Progress < _settings.WaterfallSettings.NormalFadeDarkGray)
                    return Utility.DarkenColor(_settings.WaterfallSettings.Color);
                else
                    return ConsoleColor.Black;
            }
        }
    }
}

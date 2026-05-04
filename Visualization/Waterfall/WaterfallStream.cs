using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Core;

namespace TERMINAL_FREQUENCY.Visualization.Waterfall
{
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

        public float Progress; //0 is origin, 1 is furthest edge
        public float Life; //1 is full life, 0 is dead
        public float Intensity;
        public bool IsReversed;
        public VisualizationOrigin Origin;

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

        public void Update()
        {
            Progress += _settings.WaterfallSettings.Speed * 0.025f; //0.025f is speed scaling factor
            if(Progress > 1.0f) Progress = 1.0f;
            Life -= _settings.WaterfallSettings.FadeRate;
            if (Progress >= 1.0f)
                Life = 0;
        }

        public bool IsAlive => Life > 0 && Progress < 1.0f;

        public float GetWidth(float consoleSize)
        {
            float startWidth = consoleSize * _settings.WaterfallSettings.StartWidthPercent;
            float endWidth = consoleSize * _settings.WaterfallSettings.EndWidthPercent;

            //linear interp
            return startWidth + (endWidth - startWidth) * Progress;
        }

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

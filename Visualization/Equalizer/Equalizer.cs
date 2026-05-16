using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Config.Settings.Visualizations;
using TERMINAL_FREQUENCY.Core.Rendering;

namespace TERMINAL_FREQUENCY.Visualization.Equalizer
{
    public class Equalizer : IFrequencyReactive
    {
        private Settings _settings;
        private float[] _smoothedBands;
        private string _name = "EQUALIZER";
        private int _modeIndex = 3;

        public string Name => _name;
        public int ModeIndex => _modeIndex;

        public Equalizer(Settings settings)
        {
            _settings = settings;
            _smoothedBands = new float[_settings.Fft.BandCount];
        }

        public void OnFrequencyData(float[] bands)
        {
            if (bands is null) return;

            //reset band array for setting change detection
            if (_smoothedBands.Length < bands.Length)
                Array.Resize(ref _smoothedBands, bands.Length);

            for (int i = 0; i < bands.Length && i < _smoothedBands.Length; i++)
            {
                if (_settings.Equalizer.SmoothMode)
                {
                    _smoothedBands[i] += (bands[i] - _smoothedBands[i]) * _settings.Equalizer.LerpFactor;
                    _smoothedBands[i] *= 0.995f; //slight decay so bands always moving
                }
                else
                {
                    _smoothedBands[i] = bands[i];
                }
                _smoothedBands[i] = Math.Clamp(_smoothedBands[i], 0f, 1f);
            }
        }
        public void Draw(ScreenBuffer buffer)
        {
            EqualizerSettings eq = _settings.Equalizer;
            int displayBands = _settings.Fft.BandCount;
            int dataBands = eq.Direction == EqDirection.Mirror ? displayBands / 2 : displayBands;
            int barSpacing = eq.BandSpacing;

            bool isCentered = eq.Origin == VisualizationOrigin.Center;
            bool useHorizontal = isCentered && eq.HorizontalWhenCentered;
            bool isHorizontal = eq.Origin == VisualizationOrigin.Left || eq.Origin == VisualizationOrigin.Right || useHorizontal;

            float[] bands = GetOrderedBands(dataBands);

            if (isHorizontal)
                DrawHorizontal(bands, eq, displayBands, dataBands, barSpacing, buffer);
            else
                DrawVertical(bands, eq, displayBands, dataBands, barSpacing, buffer);
        }

        private void DrawVertical(float[] bands, EqualizerSettings eq, int displayBands, int dataBands, int barSpacing, ScreenBuffer buffer)
        {
            int totalWidth = buffer.Width - 2;
            int totalSpacing = (displayBands - 1) * barSpacing;
            int barWidth = Math.Max(1, (totalWidth - totalSpacing) / displayBands);
            int usedWidth = displayBands * barWidth + totalSpacing;
            int startX = (buffer.Width - usedWidth) / 2;

            bool isCentered = eq.Origin == VisualizationOrigin.Center;
            int maxHeight = (int)(buffer.Height * eq.MaxBandHeightPercent);
            if (isCentered) maxHeight /= 2;
            int minHeight = (int)(buffer.Height * eq.MinBandHeightPercent);

            int baseY;
            bool fromTop;

            if (isCentered)
            {
                baseY = buffer.Height / 2;
                fromTop = true;
            }
            else
            {
                baseY = eq.Origin == VisualizationOrigin.Top ? minHeight + 1 : buffer.Height - 2;
                fromTop = eq.Origin == VisualizationOrigin.Top;
            }

            for (int i = 0; i < displayBands; i++)
            {
                int dataIndex = (eq.Direction == EqDirection.Mirror && i >= dataBands)
                    ? displayBands - 1 - i
                    : i;

                float bandValue = dataIndex < bands.Length ? bands[dataIndex] : 0f;
                int barHeight = minHeight + (int)((maxHeight - minHeight) * bandValue);
                int barX = startX + i * (barWidth + barSpacing);

                ConsoleColor color = GetBandColor(dataIndex);

                if (isCentered)
                {
                    DrawBar(buffer, barX, baseY, barWidth, barHeight, color, fromTop: true);
                    DrawBar(buffer, barX, baseY - 1, barWidth, barHeight, color, fromTop: false);
                }
                else
                {
                    DrawBar(buffer, barX, baseY, barWidth, barHeight, color, fromTop);
                }
            }
        }

        private void DrawHorizontal(float[] bands, EqualizerSettings eq, int displayBands, int dataBands, int barSpacing, ScreenBuffer buffer)
        {
            int totalHeight = buffer.Height - 4;
            int totalSpacing = (displayBands - 1) * barSpacing;
            int barHeight = Math.Max(1, (totalHeight - totalSpacing) / displayBands);
            int usedHeight = displayBands * barHeight + totalSpacing;
            int startY = (buffer.Height - usedHeight) / 2;

            bool isCentered = eq.Origin == VisualizationOrigin.Center;
            int maxLength = (int)(buffer.Width * eq.MaxBandHeightPercent);
            if (isCentered) maxLength /= 2;
            int minLength = (int)(buffer.Width * eq.MinBandHeightPercent);

            int baseX;
            bool fromLeft;

            if (isCentered)
            {
                baseX = buffer.Width / 2;
                fromLeft = true;
            }
            else
            {
                baseX = eq.Origin == VisualizationOrigin.Left ? minLength + 1 : buffer.Width - 2;
                fromLeft = eq.Origin == VisualizationOrigin.Left;
            }

            for (int i = 0; i < displayBands; i++)
            {
                int dataIndex = (eq.Direction == EqDirection.Mirror && i >= dataBands)
                    ? displayBands - 1 - i
                    : i;

                float bandValue = dataIndex < bands.Length ? bands[dataIndex] : 0f;
                int barLength = minLength + (int)((maxLength - minLength) * bandValue);
                int barY = startY + i * (barHeight + barSpacing);

                ConsoleColor color = GetBandColor(dataIndex);

                if (isCentered)
                {
                    DrawHorizontalBar(buffer, baseX, barY, barLength, barHeight, color, fromLeft: true);
                    DrawHorizontalBar(buffer, baseX - 1, barY, barLength, barHeight, color, fromLeft: false);
                }
                else
                {
                    DrawHorizontalBar(buffer, baseX, barY, barLength, barHeight, color, fromLeft);
                }
            }
        }

        private void DrawHorizontalBar(ScreenBuffer buffer, int baseX, int y, int length, int height, ConsoleColor color, bool fromLeft)
        {
            for (int h = 0; h < height; h++)
            {
                for (int l = 0; l < length; l++)
                {
                    int drawX = fromLeft ? baseX + l : baseX - l;
                    bool isEdge = !_settings.Equalizer.SolidBands && (l == 0 || l == length - 1 || h == 0 || h == height - 1);

                    if (_settings.Equalizer.SolidBands || isEdge)
                        buffer.SetPixel(drawX, y + h, _settings.Equalizer.BandCharacter, color);
                }
            }
        }

        private float[] GetOrderedBands(int dataBands)
        {
            float[] bands = new float[dataBands];
            int copyLength = Math.Min(_smoothedBands.Length, dataBands);
            Array.Copy(_smoothedBands, bands, copyLength);

            if (_settings.Equalizer.Direction == EqDirection.HighToLow)
                Array.Reverse(bands);

            return bands;
        }

        private ConsoleColor GetBandColor(int index)
        {
            ConsoleColor[] gradient = _settings.Equalizer.GradientColors;

            switch (_settings.Equalizer.ColorMode)
            {
                case EqColorMode.Uniform:
                    return _settings.Equalizer.UniformColor;
                case EqColorMode.Pattern:
                    var pattern = _settings.Equalizer.ColorPattern;
                    return pattern.Length > 0 ? pattern[index % pattern.Length] : ConsoleColor.White;
                case EqColorMode.Gradient:
                    float value = _smoothedBands[index % _smoothedBands.Length];
                    if (value < 0.33f) return gradient[0];
                    if (value < 0.66f) return gradient[1];
                    return gradient[2];
            }
            return ConsoleColor.White;
        }

        private void DrawBar(ScreenBuffer buffer, int x, int baseY, int width, int height, ConsoleColor color, bool fromTop)
        {
            for (int w = 0; w < width; w++)
            {
                for (int h = 0; h < height; h++)
                {
                    int drawY = fromTop ? baseY + h : baseY - h;
                    bool isEdge = !_settings.Equalizer.SolidBands && (h == 0 || h == height - 1 || w == 0 || w == width - 1);

                    if (_settings.Equalizer.SolidBands || isEdge)
                        buffer.SetPixel(x + w, drawY, _settings.Equalizer.BandCharacter, color);
                }
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Config.Settings.Visualizations;
using TERMINAL_FREQUENCY.Core.Rendering;

namespace TERMINAL_FREQUENCY.Visualization.NoiseField
{
    public class NoiseField : IVolumeReactive
    {
        private Settings _settings;
        private string _name = "NOISEFIELD";
        private int _modeIndex = 5;
        private float _currentVolume = 0f;
        private int _frameSkip = 0;
        private float _volumeAverage = 5f;
        private Random _rnd = new Random();
        private List<(int x, int y, char c)> _particles = new();

        string IVisualization.Name => _name;
        int IVisualization.ModeIndex => _modeIndex;

        public NoiseField(Settings settings)
        {
            _settings = settings;
        }

        public void Update(float volume)
        {
            var nf = _settings.NoiseField;

            _volumeAverage = _volumeAverage * 0.99f + volume * 0.01f;

            float thresholdValue = _volumeAverage * (1f + nf.VolumeThreshold / nf.Sensitivity);

            float reactiveVolume;
            if (volume > thresholdValue)
                reactiveVolume = volume;
            else
                reactiveVolume = 0f;

            if (reactiveVolume > _currentVolume)
                _currentVolume = reactiveVolume;
            _currentVolume *= nf.DecayRate;
            if (_currentVolume < 0.001f) _currentVolume = 0f;
        }

        public void Draw(ScreenBuffer buffer)
        {
            NoiseFieldSettings nf = _settings.NoiseField;
            int width = buffer.Width;
            int height = buffer.Height;
            int centerX = width / 2;
            int centerY = height / 2;

            float density;
            if (_volumeAverage > 0 && _currentVolume > 0)
            {
                float normalizedVolume = Math.Clamp(_currentVolume / (_volumeAverage * 2f), 0f, 1f);
                density = nf.MinDensity + (nf.MaxDensity - nf.MinDensity) * normalizedVolume;
            }
            else
            {
                density = nf.MinDensity;
            }
            int particleCount = (int)(width * height * density);
            float spread = nf.SpreadRadius * Math.Min(width, height) / 2;

            _frameSkip++;
            int changeInterval = (int)(1f / Math.Max(0.01f, nf.CharacterChangeRate));
            bool changeChars = _frameSkip >= changeInterval;
            if (changeChars) _frameSkip = 0;

            for (int i = 0; i < particleCount; i++)
            {
                if (_rnd.NextDouble() > nf.CharacterChangeRate)
                    continue;
                int posX, posY;

                if (nf.CenterOrigin)
                {
                    double angle = _rnd.NextDouble() * 2 * Math.PI;
                    double radius = spread * Math.Sqrt(_rnd.NextDouble());
                    posX = centerX + (int)(Math.Cos(angle) * radius);
                    posY = centerY + (int)(Math.Sin(angle) * radius * 0.45f);
                }
                else
                {
                    posX = _rnd.Next(width);
                    posY = _rnd.Next(height);
                }

                if (nf.JitterAmount > 0 && _currentVolume > 0.1f)
                {
                    int jitter = (int)(_currentVolume * nf.JitterAmount * 5);
                    posX += _rnd.Next(-jitter, jitter + 1);
                    posY += _rnd.Next(-jitter, jitter + 1);
                }

                posX = Math.Clamp(posX, 0, width - 1);
                posY = Math.Clamp(posY, 0, height - 1);

                string charSet = nf.CharacterSet;
                if (nf.UseDualCharacterSets)
                    charSet = _currentVolume > nf.CharacterSwitchThreshold ? nf.LoudCharacterSet : nf.QuietCharacterSet;

                char c = charSet[_rnd.Next(charSet.Length)];
                ConsoleColor color = (nf.UseColorPattern && nf.ColorPattern.Length > 0) ? nf.ColorPattern[_rnd.Next(nf.ColorPattern.Length)] : nf.Color;
                buffer.SetPixel(posX, posY, c, color);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Core;

namespace TERMINAL_FREQUENCY.Visualization.Rings
{
    public class Rings : IVisualization
    {
        private Settings _settings;
        private List<Ring> _rings = new List<Ring>();
        private readonly object _ringLock = new object();
        private int _maxRings;
        private float _smoothedVolume = 0;
        private string _name = "RINGS";
        private int _modeIndex = 0;

        string IVisualization.Name => _name;
        int IVisualization.ModeIndex => _modeIndex;

        public Rings(Settings settings)
        {
            _settings = settings;
            _maxRings = _settings.RingsSettings.MaxRings;
        }
        public void Update(float volume)
        {
            _smoothedVolume = volume;

            lock (_ringLock)
            {
                //update _rings
                for (int i = _rings.Count - 1; i >= 0; i--)
                {
                    _rings[i].Update(_settings.RingsSettings.Speed, _settings.RingsSettings.FadeRate);

                    if (!_rings[i].IsAlive)
                        _rings.RemoveAt(i);
                }
            }
        }

        public void OnSpike()
        {
            lock (_ringLock)
            {
                if (_rings.Count >= _maxRings)
                    _rings.RemoveAt(0);
                _rings.Add(new Ring(_settings));
            }
        }

        public void Draw(ScreenBuffer buffer)
        {
            int centerX = buffer.Width / _settings.RingsSettings.Offset;
            int centerY = buffer.Height / _settings.RingsSettings.Offset;

            if(_settings.RingsSettings.DrawCrosshair)
                buffer.SetPixel(centerX, centerY, _settings.RingsSettings.CrosshairChar, _settings.RingsSettings.CrosshairColor);

            float ambientRadius = _settings.RingsSettings.AmbientBaseRadius + _smoothedVolume * _settings.RingsSettings.AmbientVolumeMultiplier;
            if (ambientRadius > _settings.RingsSettings.AmbientRadiusMax) ambientRadius = _settings.RingsSettings.AmbientRadiusMax;

            for (int segmentIndex = 0; segmentIndex < _settings.RingsSettings.AmbientSegments; segmentIndex++)
            {
                double angle = segmentIndex * 2 * Math.PI / _settings.RingsSettings.AmbientSegments;
                int x = centerX + (int)(Math.Cos(angle) * ambientRadius);
                int y = centerY + (int)(Math.Sin(angle) * ambientRadius * _settings.RingsSettings.YStretch);

                if (_settings.RingsSettings.DrawCrosshair && segmentIndex % _settings.RingsSettings.AmbientDotInterval == 0)
                    buffer.SetPixel(x, y, _settings.RingsSettings.CrosshairCharOutter, _settings.RingsSettings.CrosshairColor);
            }

            //draw _rings - create a copy to avoid holding lock during rendering
            List<Ring> ringsCopy;
            lock (_ringLock)
            {
                ringsCopy = new List<Ring>(_rings);
            }

            for (int ringIndex = 0; ringIndex < ringsCopy.Count; ringIndex++)
            {
                var ring = ringsCopy[ringIndex];
                int segments = _settings.RingsSettings.Segments;
                for (int i = 0; i < segments; i++)
                {
                    double angle = i * 2 * Math.PI / segments;
                    int x = centerX + (int)(Math.Cos(angle) * ring.Radius);
                    int y = centerY + (int)(Math.Sin(angle) * ring.Radius * _settings.RingsSettings.YStretch);

                    //only draw if within buffer bounds
                    buffer.SetPixel(x, y, ring.GetChar(i), ring.GetColor());
                }
            }
        }
    }
}
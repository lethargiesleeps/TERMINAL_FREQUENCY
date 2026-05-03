using System;
using System.Collections.Generic;
using TERMINAL_FREQUENCY.Core;

namespace TERMINAL_FREQUENCY.Visualization.Rings
{
    public class Rings : IVisualization
    {
        private List<Ring> _rings = new List<Ring>();
        private readonly object _ringLock = new object();
        private int _maxRings = Config.Config.RINGS_MAX; //TODO: maybe just make adjustable at OnSpike
        private float _smoothedVolume = 0;
        private string _name = "RINGS";
        private int _modeIndex = 0;

        string IVisualization.Name => _name;
        int IVisualization.ModeIndex => _modeIndex;

        public void Update(float volume)
        {
            _smoothedVolume = volume;

            lock (_ringLock)
            {
                //update _rings
                for (int i = _rings.Count - 1; i >= 0; i--)
                {
                    _rings[i].Update(Config.Config.RING_SPEED, Config.Config.RING_FADE_RATE);

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
                _rings.Add(new Ring());
            }
        }

        public void Draw(ScreenBuffer buffer)
        {
            int centerX = buffer.Width / Config.Config.RING_OFFSET;
            int centerY = buffer.Height / Config.Config.RING_OFFSET;

            if(Config.Config.RINGS_DRAW_CROSSHAIR)
                buffer.SetPixel(centerX, centerY, Config.Config.RINGS_CROSSHAIR_CHAR, Config.Config.RINGS_CROSSHAIR_COLOR);

            float ambientRadius = Config.Config.RING_AMBIENT_BASE_RADIUS + _smoothedVolume * Config.Config.RING_AMBIENT_VOLUME_MULTIPLIER;
            if (ambientRadius > Config.Config.RING_AMBIENT_RADIUS_MAX) ambientRadius = Config.Config.RING_AMBIENT_RADIUS_MAX;

            for (int segmentIndex = 0; segmentIndex < Config.Config.RING_AMBIENT_SEGMENTS; segmentIndex++)
            {
                double angle = segmentIndex * 2 * Math.PI / Config.Config.RING_AMBIENT_SEGMENTS;
                int x = centerX + (int)(Math.Cos(angle) * ambientRadius);
                int y = centerY + (int)(Math.Sin(angle) * ambientRadius * Config.Config.RING_Y_STRETCH);

                if (Config.Config.RINGS_DRAW_CROSSHAIR && segmentIndex % Config.Config.RING_AMBIENT_DOT_INTRVAL == 0)
                    buffer.SetPixel(x, y, Config.Config.RINGS_CROSSHAIR_CHAR_AMBIENT, Config.Config.RING_AMBIENT_COLOR);
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
                int segments = Config.Config.RING_SEGMENTS;
                for (int i = 0; i < segments; i++)
                {
                    double angle = i * 2 * Math.PI / segments;
                    int x = centerX + (int)(Math.Cos(angle) * ring.Radius);
                    int y = centerY + (int)(Math.Sin(angle) * ring.Radius * Config.Config.RING_Y_STRETCH);

                    //only draw if within buffer bounds
                    buffer.SetPixel(x, y, ring.GetChar(i), ring.GetColor());
                }
            }
        }
    }
}
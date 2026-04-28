using System;
using System.Collections.Generic;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Visualization;

namespace TERMINAL_FREQUENCY.Visualization
{
    public class Rings : IVisualization
    {
        private List<Ring> rings = new List<Ring>();
        private readonly object ringLock = new object();
        private int maxRings = Config.Config.RINGS_MAX;
        private float smoothedVolume = 0;
        private string name = "RINGS";
        private int modeIndex = 0;

        string IVisualization.Name => name;
        int IVisualization.ModeIndex => modeIndex;

        public void Update(float volume)
        {
            smoothedVolume = volume;

            lock (ringLock)
            {
                //update rings
                for (int i = rings.Count - 1; i >= 0; i--)
                {
                    rings[i].Update(Config.Config.RING_SPEED, Config.Config.RING_FADE_RATE);

                    if (!rings[i].IsAlive)
                        rings.RemoveAt(i);
                }
            }
        }

        public void OnSpike()
        {
            lock (ringLock)
            {
                if (rings.Count >= maxRings)
                    rings.RemoveAt(0);
                rings.Add(new Ring());
            }
        }

        public void Draw(ScreenBuffer buffer)
        {
            int centerX = buffer.Width / Config.Config.RING_OFFSET;
            int centerY = buffer.Height / Config.Config.RING_OFFSET;

            if(Config.Config.RINGS_DRAW_CROSSHAIR)
                buffer.SetPixel(centerX, centerY, Config.Config.RINGS_CROSSHAIR_CHAR, Config.Config.RINGS_CROSSHAIR_COLOR);

            float ambientRadius = Config.Config.RING_AMBIENT_BASE_RADIUS + smoothedVolume * Config.Config.RING_AMBIENT_VOLUME_MULTIPLIER;
            if (ambientRadius > Config.Config.RING_AMBIENT_RADIUS_MAX) ambientRadius = Config.Config.RING_AMBIENT_RADIUS_MAX;

            for (int segmentIndex = 0; segmentIndex < Config.Config.RING_AMBIENT_SEGMENTS; segmentIndex++)
            {
                double angle = (segmentIndex * 2 * Math.PI) / Config.Config.RING_AMBIENT_SEGMENTS;
                int x = centerX + (int)(Math.Cos(angle) * ambientRadius);
                int y = centerY + (int)(Math.Sin(angle) * ambientRadius * Config.Config.RING_Y_STRETCH);

                //TODO: determine if this should be in crosshair mode, make the . configurable
                if (segmentIndex % Config.Config.RING_AMBIENT_DOT_INTRVAL == 0)
                    buffer.SetPixel(x, y, '·', Config.Config.RING_AMBIENT_COLOR);
            }

            //draw rings - create a copy to avoid holding lock during rendering
            List<Ring> ringsCopy;
            lock (ringLock)
            {
                ringsCopy = new List<Ring>(rings);
            }

            for (int ringIndex = 0; ringIndex < ringsCopy.Count; ringIndex++)
            {
                var ring = ringsCopy[ringIndex];
                int segments = Config.Config.RING_SEGMENTS;
                for (int i = 0; i < segments; i++)
                {
                    double angle = (i * 2 * Math.PI) / segments;
                    int x = centerX + (int)(Math.Cos(angle) * ring.Radius);
                    int y = centerY + (int)(Math.Sin(angle) * ring.Radius * Config.Config.RING_Y_STRETCH);

                    //only draw if within buffer bounds
                    buffer.SetPixel(x, y, ring.GetChar(i), ring.GetColor());
                }
            }
        }
    }
}
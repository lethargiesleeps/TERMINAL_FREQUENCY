using System;
using System.Collections.Generic;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Visualization;

namespace TERMINAL_FREQUENCY.Visualization
{
    public class KickCircle : IVisualization
    {
        private List<Ring> rings = new List<Ring>();
        private readonly object ringLock = new object();
        private int maxRings = 5;
        private float smoothedVolume = 0;
        private string name = "KICK_CIRCLE";
        private int modeIndex = 0;

        string IVisualization.Name => name;
        int IVisualization.ModeIndex => modeIndex;

        public void Update(float volume)
        {
            smoothedVolume = volume;

            lock (ringLock)
            {
                // Update existing rings
                for (int i = rings.Count - 1; i >= 0; i--)
                {
                    rings[i].Update();

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
            int cx = buffer.Width / 2;
            int cy = buffer.Height / 2;

            buffer.SetPixel(cx, cy, '+', ConsoleColor.DarkGray);

            float ambR = 5 + smoothedVolume * 3f;
            if (ambR > 20) ambR = 20;

            for (int i = 0; i < 40; i++)
            {
                double angle = (i * 2 * Math.PI) / 40;
                int x = cx + (int)(Math.Cos(angle) * ambR);
                int y = cy + (int)(Math.Sin(angle) * ambR * 0.45);

                if (i % 4 == 0)
                    buffer.SetPixel(x, y, '·', ConsoleColor.DarkGray);
            }

            // Draw rings - create a copy to avoid holding lock during rendering
            List<Ring> ringsCopy;
            lock (ringLock)
            {
                ringsCopy = new List<Ring>(rings);
            }

            for (int r = 0; r < ringsCopy.Count; r++)
            {
                var ring = ringsCopy[r];
                int segments = 24;
                for (int i = 0; i < segments; i++)
                {
                    double angle = (i * 2 * Math.PI) / segments;
                    int x = cx + (int)(Math.Cos(angle) * ring.Radius);
                    int y = cy + (int)(Math.Sin(angle) * ring.Radius * 0.45);

                    // Only draw if within buffer bounds
                    buffer.SetPixel(x, y, ring.GetChar(i), ring.GetColor());
                }
            }
        }
    }
}
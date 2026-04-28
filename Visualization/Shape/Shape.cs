using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Core;

namespace TERMINAL_FREQUENCY.Visualization.Shape
{
    public class Shape : IVisualization
    {
        private string name = "SHAPE";
        private int modeIndex = 2;
        private float currentSize = 0f;
        private float targetSize = 0f;
        private float smoothedVolume = 0f;
        private readonly object shapeLock = new object();

        string IVisualization.Name => name;
        int IVisualization.ModeIndex => modeIndex;
        public bool IsReversed { get; set; }
        public bool IsSmoothingEnabled { get; set; }
        public bool IsCustomColorEnabled { get; set; }

        public Shape() 
        {
            IsReversed = Config.Config.SHAPE_REVERSE_MODE;
            IsSmoothingEnabled = Config.Config.SHAPE_SMOOTH_MODE;
            IsCustomColorEnabled = Config.Config.SHAPE_USE_CUSTOM_COLOR;
        }

        public void Update(float volume)
        {
            IsReversed = Config.Config.SHAPE_REVERSE_MODE;
            IsSmoothingEnabled = Config.Config.SHAPE_SMOOTH_MODE;
            IsCustomColorEnabled = Config.Config.SHAPE_USE_CUSTOM_COLOR;

            if (volume < Config.Config.SHAPE_TRIGGER_THRESHOLD)
                volume = 0;

            smoothedVolume = volume;
            float maxSize = GetEffectiveMaxSize();
            float minSize = GetMinSize();

            targetSize = IsReversed
                ? maxSize - (volume * (maxSize - minSize))
                : minSize + (volume * (maxSize - minSize));

            currentSize = IsSmoothingEnabled
                ? currentSize + (targetSize - currentSize) * Config.Config.SHAPE_LERP_FACTOR
                : targetSize;
        }

        public void OnSpike()
        {
            targetSize = IsReversed ? GetMinSize() : GetEffectiveMaxSize();
            if(!IsSmoothingEnabled) currentSize = targetSize;
        }

        public void Draw(ScreenBuffer buffer)
        {
            switch(Config.Config.SHAPE_LAYOUT)
            {
                case ShapeLayout.Concentric: DrawConcentric(buffer); break;
                case ShapeLayout.Single:
                default:
                    DrawSingle(buffer);
                    break;
            }
        }

        private void DrawSingle(ScreenBuffer buffer)
        {
            int centerX = buffer.Width / 2;
            int centerY = buffer.Height / 2;
            int maxDimension = Math.Min(buffer.Width, buffer.Height);
            int radius = (int)(maxDimension * currentSize / 2);
            int thickness = GetEffectiveThickness(1);

            DrawShapeAt(buffer, centerX, centerY, radius, thickness, GetColor(0));
        }

        private void DrawConcentric(ScreenBuffer buffer)
        {
            int centerX = buffer.Width / 2;
            int centerY = buffer.Height / 2;
            int count = Config.Config.SHAPE_COUNT;
            int padding = Config.Config.SHAPE_PADDING;
            int thickness = GetEffectiveThickness(count);
            int maxDimension = Math.Min(buffer.Width, buffer.Height);

            //outer shape
            int outerRadius = (int)(maxDimension * currentSize / 2);

            //min size needed to fit all rings without collapsing
            int minOuter = (count * (thickness + padding)) + padding;
            if (outerRadius < minOuter)
                outerRadius = minOuter;

            //space between each ring
            int totalStep = thickness + padding;
            int radiusStep = count > 1 ? (outerRadius - thickness) / count : 0;

            for (int i = 0; i < count; i++)
            {
                int radius = outerRadius - (i * radiusStep);
                if (radius < 3) radius = 3; //never smaller than a visible circle
                DrawShapeAt(buffer, centerX, centerY, radius, thickness, GetColor(i));
            }
        }

        private void DrawShapeAt(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            if (radius <= 0) return;

            switch (Config.Config.SHAPE_TYPE)
            {
                case ShapeType.Circle:
                    DrawCircle(buffer, centerX, centerY, radius, thickness, color);
                    break;
                case ShapeType.Square:
                    DrawCircle(buffer, centerX, centerY, radius, thickness, color); // Placeholder
                    break;
                case ShapeType.Diamond:
                    DrawCircle(buffer, centerX, centerY, radius, thickness, color); // Placeholder
                    break;
                case ShapeType.Hexagon:
                    DrawCircle(buffer, centerX, centerY, radius, thickness, color); // Placeholder
                    break;
            }
        }
        
        private float GetEffectiveMaxSize()
        {
            if (Config.Config.SHAPE_LAYOUT == ShapeLayout.Concentric)
                return Config.Config.SHAPE_MAX_SIZE_PERCENT;
            else
            {
                int count = Math.Max(1, Config.Config.SHAPE_COUNT);
                return Config.Config.SHAPE_MAX_SIZE_PERCENT / count;
            }
        }

        private float GetMinSize() => Config.Config.SHAPE_MIN_SIZE_PERCENT;

        private int GetEffectiveThickness(int shapeCount)
        {
            int thickness = Config.Config.SHAPE_THICKNESS;
            int maxThickness = Config.Config.SHAPE_THICKNESS_MAX;

            thickness = Math.Max(1, thickness / shapeCount);
            return Math.Min(thickness, maxThickness);
        }

        private ConsoleColor GetColor(int shapeIndex)
        {
            if(IsCustomColorEnabled)
            {
                ConsoleColor[] colors = Config.Config.SHAPE_CUSTOM_COLORS;
                if (colors != null && colors.Length > 0)
                    return colors[shapeIndex % colors.Length];
            }

            return Config.Config.SHAPE_UNIFORM_COLOR;
        }

        private void DrawCircle(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            int innerRadius = Math.Max(0, radius - thickness);

            for(int r = innerRadius; r <= radius; r++)
            {
                if (r == 0) continue;

                int segments = (int)(2 * Math.PI * r * Config.Config.SHAPE_CIRCLE_SEGMENT_DENSITY);
                if(segments < Config.Config.SHAPE_CIRCLE_MIN_SEGMENTS) segments = Config.Config.SHAPE_CIRCLE_MIN_SEGMENTS;
                if (segments > Config.Config.SHAPE_CIRCLE_MAX_SEGMENTS) segments = Config.Config.SHAPE_CIRCLE_MAX_SEGMENTS;

                for(int i = 0; i < segments; i++)
                {
                    double angle = (i * 2 * Math.PI) / segments;
                    int x = centerX + (int)(Math.Cos(angle) * r);
                    int y = centerY + (int)(Math.Sin(angle) * r * 0.45);

                    buffer.SetPixel(x, y, Config.Config.SHAPE_CHARACTER, color);
                }
            }

            //draw solid center if near min size
            if (radius <= thickness + 1)
            {
                buffer.SetPixel(centerX, centerY, Config.Config.SHAPE_CHARACTER, color);
                buffer.SetPixel(centerX + 1, centerY, Config.Config.SHAPE_CHARACTER, color);
                buffer.SetPixel(centerX, centerY + 1, Config.Config.SHAPE_CHARACTER, color);
                buffer.SetPixel(centerX - 1, centerY, Config.Config.SHAPE_CHARACTER, color);
                buffer.SetPixel(centerX, centerY - 1, Config.Config.SHAPE_CHARACTER, color);
            }
        }
    }
}

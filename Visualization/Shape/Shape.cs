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

            FillShape(buffer, centerX, centerY, radius, thickness, 0); //fill first
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

            int outerRadius = (int)(maxDimension * currentSize / 2);

            //ensure rings don't collapse, but respect low volume
            int minOuter = (count * (thickness + padding)) + padding;
            int calculatedOuter = (int)(maxDimension * currentSize / 2);
            if (calculatedOuter < minOuter && currentSize > 0.05f)
                outerRadius = minOuter;
            else
                outerRadius = calculatedOuter;

            //space between each ring
            int totalStep = thickness + padding;
            int radiusStep = count > 1 ? (outerRadius - thickness) / count : 0;

            for (int i = 0; i < count; i++)
            {
                int radius = outerRadius - (i * radiusStep);
                if (radius < 3) radius = 3; //never smaller than a visible circle
                FillShape(buffer, centerX, centerY, radius, thickness, i); //fill first
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
                    DrawSquare(buffer, centerX, centerY, radius, thickness, color);
                    break;
                case ShapeType.Diamond:
                    DrawDiamond(buffer, centerX, centerY, radius, thickness, color);
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

        private void DrawSquare(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            int halfWidth = (int)(radius * Config.Config.SHAPE_SQUARE_WIDTH_RATIO);
            int halfHeight = (int)(radius * Config.Config.SHAPE_SQUARE_HEIGHT_RATIO * 0.45f); //aspect ratio correction

            for(int t = 0; t < thickness; t++)
            {
                int top = centerY - halfHeight + t;
                int bottom = centerY + halfHeight - t;
                int left = centerX - halfWidth + t;
                int right = centerX + halfWidth - t;

                //top edge
                for (int x = left; x <= right; x++)
                    buffer.SetPixel(x, top, Config.Config.SHAPE_CHARACTER, color);

                //bottom edge
                for (int x = left; x <= right; x++)
                    buffer.SetPixel(x, bottom, Config.Config.SHAPE_CHARACTER, color);

                //left edge
                for (int y = top; y <= bottom; y++)
                    buffer.SetPixel(left, y, Config.Config.SHAPE_CHARACTER, color);

                //right edge
                for (int y = top; y <= bottom; y++)
                    buffer.SetPixel(right, y, Config.Config.SHAPE_CHARACTER, color);
            }

            //soliud center if small
            if (radius <= thickness + 1)
            {
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        buffer.SetPixel(centerX + dx, centerY + dy, Config.Config.SHAPE_CHARACTER, color);
            }
        }

        private void DrawDiamond(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            int halfWidth = radius;
            int halfHeight = (int)(radius * 0.45f); //correct based on aspect ratio

            for(int t = 0; t < thickness;  t++)
            {
                int hw = halfWidth - t; //calculated half width
                int hh = halfHeight - t; //calculated half height

                int topX = centerX;
                int topY = centerY - hh;

                int rightX = centerX + hw;
                int rightY = centerY;

                int bottomX = centerX;
                int bottomY = centerY + hh;

                int leftX = centerX - hw;
                int leftY = centerY;

                //draw the diamond edges
                DrawLine(buffer, topX, topY, rightX, rightY, color);    //top to right
                DrawLine(buffer, rightX, rightY, bottomX, bottomY, color); //right to bottom
                DrawLine(buffer, bottomX, bottomY, leftX, leftY, color);   //bottom to left
                DrawLine(buffer, leftX, leftY, topX, topY, color);         //left to top
            }
        }

        private void DrawLine(ScreenBuffer buffer, int x0, int y0, int x1, int y1, ConsoleColor color)
        {
            //draw the line from x0/y0 to x1/y1 using bresenham algo
            int deltaX = Math.Abs(x1 - x0);
            int deltaY = Math.Abs(y1 - y0);
            int stepX = x0 < x1 ? 1 : -1;
            int stepY = y0 < y1 ? 1 : -1;
            int error = deltaX - deltaY;

            while(true)
            {
                buffer.SetPixel(x0, y0, Config.Config.SHAPE_CHARACTER, color);

                if (x0 == x1 && y0 == y1) break;

                int doubleError = 2 * error;
                if(doubleError > -deltaY)
                {
                    error -= deltaY;
                    x0 += stepX;
                }

                if(doubleError < deltaX)
                {
                    error += deltaX;
                    y0 += stepY;
                }
            }
        }

        private void FillShape(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, int shapeIndex)
        {
            if (!Config.Config.SHAPE_FILL_MODE) return;

            char fillChar = Config.Config.SHAPE_FILL_CHARACTERS[shapeIndex % Config.Config.SHAPE_FILL_CHARACTERS.Length];
            ConsoleColor fillColor = Config.Config.SHAPE_FILL_COLORS[shapeIndex % Config.Config.SHAPE_FILL_COLORS.Length];
            int spacing = Config.Config.SHAPE_FILL_SPACING + 1; // 0=1=every pixel, 1=2=every other, etc

            int innerLimit = thickness; //start filling from inner edge of outline

            switch (Config.Config.SHAPE_TYPE)
            {
                case ShapeType.Circle:
                    FillCircle(buffer, centerX, centerY, radius - thickness, fillChar, fillColor, spacing);
                    break;
                case ShapeType.Square:
                    FillSquare(buffer, centerX, centerY, radius - thickness, fillChar, fillColor, spacing);
                    break;
                case ShapeType.Diamond:
                    FillDiamond(buffer, centerX, centerY, radius - thickness, fillChar, fillColor, spacing);
                    break;
            }

        }

        private void FillCircle(ScreenBuffer buffer, int centerX, int centerY, int maxRadius, char fillChar, ConsoleColor fillColor, int spacing)
        {
            for (int r = 0; r <= maxRadius; r++)
            {
                int segments = (int)(2 * Math.PI * r * Config.Config.SHAPE_CIRCLE_SEGMENT_DENSITY);
                if (segments < Config.Config.SHAPE_CIRCLE_MIN_SEGMENTS) segments = Config.Config.SHAPE_CIRCLE_MIN_SEGMENTS;
                if (segments > Config.Config.SHAPE_CIRCLE_MAX_SEGMENTS) segments = Config.Config.SHAPE_CIRCLE_MAX_SEGMENTS;

                for (int i = 0; i < segments; i += spacing)
                {
                    double angle = (i * 2 * Math.PI) / segments;
                    int x = centerX + (int)(Math.Cos(angle) * r);
                    int y = centerY + (int)(Math.Sin(angle) * r * 0.45);
                    buffer.SetPixel(x, y, fillChar, fillColor);
                }
            }
        }

        private void FillSquare(ScreenBuffer buffer, int centerX, int centerY, int radius, char fillChar, ConsoleColor fillColor, int spacing)
        {
            int halfWidth = (int)(radius * Config.Config.SHAPE_SQUARE_WIDTH_RATIO);
            int halfHeight = (int)(radius * Config.Config.SHAPE_SQUARE_HEIGHT_RATIO * 0.45f);

            for (int y = centerY - halfHeight + 1; y <= centerY + halfHeight - 1; y += spacing)
            {
                for (int x = centerX - halfWidth + 1; x <= centerX + halfWidth - 1; x += spacing)
                {
                    buffer.SetPixel(x, y, fillChar, fillColor);
                }
            }
        }

        private void FillDiamond(ScreenBuffer buffer, int centerX, int centerY, int radius, char fillChar, ConsoleColor fillColor, int spacing)
        {
            int minRadius = 3; //mybe let user adjust this, but should not go below 3
            if (radius < minRadius) return;

            int halfWidth = radius;
            int halfHeight = (int)(radius * 0.45f);

            //increase for biger diamonds
            int effectiveSpacing = spacing;
            if (radius > 25) effectiveSpacing += 1;
            if (radius > 40) effectiveSpacing += 1;

            for (int y = centerY - halfHeight; y <= centerY + halfHeight; y += effectiveSpacing)
            {
                //calculate how far from center this row is (0 to 1)
                float rowProgress = Math.Abs(y - centerY) / (float)halfHeight;

                //width at this row (diamond gets narrower toward top/bottom)
                int rowHalfWidth = (int)(halfWidth * (1 - rowProgress));

                //fill this row from left to right edge
                for (int x = centerX - rowHalfWidth; x <= centerX + rowHalfWidth; x += effectiveSpacing)
                    buffer.SetPixel(x, y, fillChar, fillColor);

            }
        }
    }
}

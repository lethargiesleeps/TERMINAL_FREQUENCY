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
        private string _name = "SHAPE";
        private int _modeIndex = 2;
        private float _currentSize = 0f;
        private float _targetSize = 0f;
        private float _peakVolume = 0.1f;
        private const float SIZE_BOOST = 2.0f; //small boost for layouts where shapes tend to get cramped, non modifiable, user can modify many other settings to get bigger shapes

        string IVisualization.Name => _name;
        int IVisualization.ModeIndex => _modeIndex;
        public bool IsReversed { get; set; }
        public bool IsSmoothingEnabled { get; set; }
        public bool IsCustomColorEnabled { get; set; }
        public bool IsCyclingEnabled { get; set; }
        
        public Shape()
        {
            IsReversed = Config.Config.SHAPE_REVERSE_MODE;
            IsSmoothingEnabled = Config.Config.SHAPE_SMOOTH_MODE;
            IsCustomColorEnabled = Config.Config.SHAPE_USE_CUSTOM_COLOR;
        }

        #region IVisualization
        public void Update(float volume)
        {
            IsReversed = Config.Config.SHAPE_REVERSE_MODE;
            IsSmoothingEnabled = Config.Config.SHAPE_SMOOTH_MODE;
            IsCustomColorEnabled = Config.Config.SHAPE_USE_CUSTOM_COLOR;

            float maxSize = GetEffectiveMaxSize();
            float minSize = GetMinSize();
            float scaledVolume;

            if (IsReversed)
            {
                //track peak and normalize for reverse
                if (volume > _peakVolume)
                    _peakVolume = volume;
                _peakVolume *= 0.995f;

                float normalizedVolume = _peakVolume > 0.01f ? Math.Clamp(volume / _peakVolume, 0f, 1f) : 0f;

                if (normalizedVolume < Config.Config.SHAPE_TRIGGER_THRESHOLD * Config.Config.SHAPE_REVERSE_VOLUME_SENSITIVITY)
                    normalizedVolume = 0;


                //boost maxSize artificially to look more like raw volume
                maxSize *= 5;

                _targetSize = maxSize - (normalizedVolume * (maxSize - minSize));
            }
            else
            {
                //raw audio volume
                if (volume < Config.Config.SHAPE_TRIGGER_THRESHOLD)
                    volume = 0;

                scaledVolume = volume * Config.Config.SHAPE_VOLUME_SENSITIVITY;

                _targetSize = minSize + (scaledVolume * (maxSize - minSize));
            }

            _currentSize = IsSmoothingEnabled
                ? _currentSize + (_targetSize - _currentSize) * Config.Config.SHAPE_LERP_FACTOR
                : _targetSize;
        }

        public void OnSpike()
        {
            _targetSize = IsReversed ? GetMinSize() : GetEffectiveMaxSize();
            if (!IsSmoothingEnabled) _currentSize = _targetSize;
        }
        #endregion

        #region LayoutMethods
        public void Draw(ScreenBuffer buffer)
        {
            switch (Config.Config.SHAPE_LAYOUT)
            {
                case ShapeLayout.Concentric: DrawConcentric(buffer); break;
                case ShapeLayout.Vertical: DrawVertical(buffer); break;
                case ShapeLayout.Horizontal: DrawHorizontal(buffer); break;
                case ShapeLayout.Pyramid: DrawPyramid(buffer); break;
                case ShapeLayout.Quadrant: DrawQuadrant(buffer); break;
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
            int radius = (int)(maxDimension * _currentSize / 2);

            int thickness = GetEffectiveThickness(1);

            FillShape(buffer, centerX, centerY, radius, thickness, 0); //fill first
            DrawShapeAt(buffer, centerX, centerY, radius, thickness, GetColor(0));
        }

        private void DrawVertical(ScreenBuffer buffer)
        {
            int count = Config.Config.SHAPE_COUNT;

            if (count == 1)
            {
                DrawSingle(buffer);
                return;
            }


            int thickness = GetEffectiveThickness(count);
            int spacing = 2; //pixel between shapes

            for (int i = 0; i < count; i++)
            {
                int centerX = buffer.Width / 2;
                int centerY = GetVerticalPosition(buffer.Height, count, i, spacing);
                int maxDimension = Math.Min(buffer.Width, buffer.Height / Math.Max(1, count));
                maxDimension = (int)(maxDimension * SIZE_BOOST);
                int radius = (int)(maxDimension * _currentSize / 2);

                FillShape(buffer, centerX, centerY, radius, thickness, i);
                DrawShapeAt(buffer, centerX, centerY, radius, thickness, GetColor(i));

            }

        }

        private void DrawHorizontal(ScreenBuffer buffer)
        {
            int count = Config.Config.SHAPE_COUNT;

            if (count == 1)
            {
                DrawSingle(buffer);
                return;
            }


            int thickness = GetEffectiveThickness(count);
            int spacing = 2;

            for (int i = 0; i < count; i++)
            {
                int centerX = GetHorizontalPosition(buffer.Width, count, i, spacing);
                int centerY = buffer.Height / 2;
                int maxDimension = Math.Min(buffer.Width / Math.Max(1, count), buffer.Height);
                int radius = (int)(maxDimension * _currentSize / 2);

                FillShape(buffer, centerX, centerY, radius, thickness, i);
                DrawShapeAt(buffer, centerX, centerY, radius, thickness, GetColor(i));
            }
        }

        private void DrawPyramid(ScreenBuffer buffer)
        {
            int count = Config.Config.SHAPE_COUNT;
            int thickness = GetEffectiveThickness(count);

            //do normal if under 3
            if (count < 3)
            {
                DrawSingle(buffer);
                return;
            }

            bool inverted = count >= 4; //if shape count is 4 we flip the pyramid
            int rows = count <= 2 ? 1 : 2;
            int shapeIndex = 0;

            for (int row = 0; row < rows; row++)
            {
                int shapesInRow;
                if (!inverted)
                    shapesInRow = row == 0 ? 1 : count - 1;  //normal
                else
                    shapesInRow = row == 0 ? count - 1 : 1;  //inverted

                int rowSpacing = (int)(buffer.Height * Config.Config.SHAPE_PYRAMID_ROW_SPACING);

                int rowY = !inverted
                    ? buffer.Height / 2 - ((rows - 1 - row) * rowSpacing)
                    : buffer.Height / 2 + (row * rowSpacing);

                for (int s = 0; s < shapesInRow; s++)
                {
                    int centerX = buffer.Width / 2;
                    if (shapesInRow > 1)
                        centerX = buffer.Width / 2 + (s == 0 ? -buffer.Width / 6 : buffer.Width / 6);

                    int maxDim = Math.Min(buffer.Width / 3, buffer.Height / 3);
                    int radius = (int)(maxDim * _currentSize / 2);

                    FillShape(buffer, centerX, rowY, radius, thickness, shapeIndex);
                    DrawShapeAt(buffer, centerX, rowY, radius, thickness, GetColor(shapeIndex));
                    shapeIndex++;
                }
            }
        }

        private void DrawQuadrant(ScreenBuffer buffer)
        {
            int count = Config.Config.SHAPE_COUNT;
            int thickness = GetEffectiveThickness(count);
            int[] indices = GetQuadrantIndices(count);
            int actualShapes = indices.Length;
            int maxDimension = Math.Min(buffer.Width, buffer.Height) / (actualShapes == 1 ? 2 : actualShapes);
            int radius = (int)(maxDimension * _currentSize / 2);
            (int x, int y)[] quads;

            if(count == 4) maxDimension = (int)(maxDimension * SIZE_BOOST);
            if (Config.Config.SHAPE_QUADRANT_CENTERED && actualShapes == 4)
            {
                //cluster to middle
                int gap = Math.Min(buffer.Width, buffer.Height) / Config.Config.SHAPE_QUADRANT_GAP_DIVISOR;
                quads = new (int, int)[]
                {
                    (buffer.Width / 2 - gap, buffer.Height / 2 - gap), //top-left of center
                    (buffer.Width / 2 + gap, buffer.Height / 2 - gap), //top-right of center
                    (buffer.Width / 2 - gap, buffer.Height / 2 + gap), //bottom-left of center
                    (buffer.Width / 2 + gap, buffer.Height / 2 + gap) //bottom-right of center
                };
            }
            else
            {
                //center of quadrants
                quads = new (int, int)[]
                {
                    (buffer.Width / 4, buffer.Height / 4),         //0: top left
                    (buffer.Width * 3 / 4, buffer.Height / 4),     //1: top-right
                    (buffer.Width / 4, buffer.Height * 3 / 4),     //2: bottom-left
                    (buffer.Width * 3 / 4, buffer.Height * 3 / 4)  //3: bottom-right
                };
            }



            for (int i = 0; i < indices.Length; i++)
            {
                int index = indices[i];
                FillShape(buffer, quads[index].x, quads[index].y, radius, thickness, i);
                DrawShapeAt(buffer, quads[index].x, quads[index].y, radius, thickness, GetColor(i));
            }
        }

        private void DrawConcentric(ScreenBuffer buffer)
        {
            int count = Config.Config.SHAPE_COUNT;

            if(count == 1)
            {
                DrawSingle(buffer);
                return;
            }

            int centerX = buffer.Width / 2;
            int centerY = buffer.Height / 2;
            
            int padding = Config.Config.SHAPE_PADDING;
            int thickness = GetEffectiveThickness(count);
            int maxDimension = Math.Min(buffer.Width, buffer.Height);

            int outerRadius = (int)(maxDimension * _currentSize / 2);

            //ensure rings don't collapse, but respect low volume
            int minOuter = (count * (thickness + padding)) + padding;
            int calculatedOuter = (int)(maxDimension * _currentSize / 2);
            if (calculatedOuter < minOuter && _currentSize > 0.05f)
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
        #endregion

        #region ShapeDrawingMethods
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
                case ShapeType.Polygon:
                    DrawPolygon(buffer, centerX, centerY, radius, thickness, color);
                    break;
                case ShapeType.TriangleUp:
                    DrawTriangleUp(buffer, centerX, centerY, radius, thickness, color);
                    break;
                case ShapeType.TriangleDown:
                    DrawTriangleDown(buffer, centerX, centerY, radius, thickness, color);
                    break;
            }
        }

        private void DrawCircle(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            int innerRadius = Math.Max(0, radius - thickness);

            for (int r = innerRadius; r <= radius; r++)
            {
                if (r == 0) continue;

                int segments = (int)(2 * Math.PI * r * Config.Config.SHAPE_CIRCLE_SEGMENT_DENSITY);
                if (segments < Config.Config.SHAPE_CIRCLE_MIN_SEGMENTS) segments = Config.Config.SHAPE_CIRCLE_MIN_SEGMENTS;
                if (segments > Config.Config.SHAPE_CIRCLE_MAX_SEGMENTS) segments = Config.Config.SHAPE_CIRCLE_MAX_SEGMENTS;

                for (int i = 0; i < segments; i++)
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

            for (int t = 0; t < thickness; t++)
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

            for (int t = 0; t < thickness; t++)
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

            while (true)
            {
                buffer.SetPixel(x0, y0, Config.Config.SHAPE_CHARACTER, color);

                if (x0 == x1 && y0 == y1) break;

                int doubleError = 2 * error;
                if (doubleError > -deltaY)
                {
                    error -= deltaY;
                    x0 += stepX;
                }

                if (doubleError < deltaX)
                {
                    error += deltaX;
                    y0 += stepY;
                }
            }
        }

        private void DrawPolygon(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            int sides = Config.Config.SHAPE_POLYGON_SIDES;

            //pre-calcuating vertices
            (int x, int y)[] vertices = new (int, int)[sides];
            for (int i = 0; i < sides; i++)
            {
                double angle = (i * 2 * Math.PI) / sides - Math.PI / 2; //start from top
                vertices[i].x = centerX + (int)(Math.Cos(angle) * radius);
                vertices[i].y = centerY + (int)(Math.Sin(angle) * radius * 0.45f);
            }

            //draw each thickness layer
            for (int t = 0; t < thickness; t++)
            {
                float scale = 1f - ((float)t / radius);
                if (scale < 0) scale = 0;

                for (int i = 0; i < sides; i++)
                {
                    int next = (i + 1) % sides;

                    int x0 = centerX + (int)((vertices[i].x - centerX) * scale);
                    int y0 = centerY + (int)((vertices[i].y - centerY) * scale);
                    int x1 = centerX + (int)((vertices[next].x - centerX) * scale);
                    int y1 = centerY + (int)((vertices[next].y - centerY) * scale);

                    DrawLine(buffer, x0, y0, x1, y1, color);
                }

                //solid center if very small
                if (radius <= thickness + 1)
                {
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            buffer.SetPixel(centerX + dx, centerY + dy, Config.Config.SHAPE_CHARACTER, color);
                }
            }
        }

        private void DrawTriangleUp(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            int sideLength = (int)(radius * Config.Config.SHAPE_TRIANGLE_SIDE_MULTIPLIER);
            int triangleHeight = (int)(sideLength * Config.Config.SHAPE_TRIANGLE_HEIGHT_MULTIPLIER * Config.Config.SHAPE_TRIANGLE_ASPECT_CORRECTION);

            for (int t = 0; t < thickness; t++)
            {
                int topX = centerX;
                int topY = centerY - triangleHeight + t;
                int leftX = centerX - sideLength / 2 + t;
                int leftY = centerY + triangleHeight - t;
                int rightX = centerX + sideLength / 2 - t;
                int rightY = centerY + triangleHeight - t;

                DrawLine(buffer, topX, topY, leftX, leftY, color);
                DrawLine(buffer, topX, topY, rightX, rightY, color);
                DrawLine(buffer, leftX, leftY, rightX, rightY, color);
            }
        }

        private void DrawTriangleDown(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            int sideLength = (int)(radius * Config.Config.SHAPE_TRIANGLE_SIDE_MULTIPLIER);
            int triangleHeight = (int)(sideLength * Config.Config.SHAPE_TRIANGLE_HEIGHT_MULTIPLIER * Config.Config.SHAPE_TRIANGLE_ASPECT_CORRECTION);

            for (int t = 0; t < thickness; t++)
            {
                int bottomX = centerX;
                int bottomY = centerY + triangleHeight - t;
                int leftX = centerX - sideLength / 2 + t;
                int leftY = centerY - triangleHeight + t;
                int rightX = centerX + sideLength / 2 - t;
                int rightY = centerY - triangleHeight + t;

                DrawLine(buffer, bottomX, bottomY, leftX, leftY, color);
                DrawLine(buffer, bottomX, bottomY, rightX, rightY, color);
                DrawLine(buffer, leftX, leftY, rightX, rightY, color);
            }
        }
        #endregion

        #region ShapeFillMethods
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
                case ShapeType.Polygon:
                    FillPolygon(buffer, centerX, centerY, radius - thickness, fillChar, fillColor, spacing);
                    break;
                case ShapeType.TriangleUp:
                    FillTriangleUp(buffer, centerX, centerY, radius - thickness, fillChar, fillColor, spacing);
                    break;
                case ShapeType.TriangleDown:
                    FillTriangleDown(buffer, centerX, centerY, radius - thickness, fillChar, fillColor, spacing);
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

        private void FillPolygon(ScreenBuffer buffer, int centerX, int centerY, int radius, char fillChar, ConsoleColor fillColor, int spacing)
        {
            if (radius < 3) return;

            int sides = Config.Config.SHAPE_POLYGON_SIDES;
            int halfHeight = (int)(radius * 0.45f);

            int effectiveSpacing = spacing;
            if (radius > 25) effectiveSpacing += 1;
            if (radius > 40) effectiveSpacing += 1;

            //scan rows top to bottom
            for (int y = centerY - halfHeight; y <= centerY + halfHeight; y += effectiveSpacing)
            {
                float rowProgress = (float)(y - centerY + halfHeight) / (halfHeight * 2);

                //calculate horizontal bounds for this row
                int rowHalfWidth = CalculatePolygonHalfWidth(radius, sides, rowProgress);

                for (int x = centerX - rowHalfWidth; x <= centerX + rowHalfWidth; x += effectiveSpacing)
                    buffer.SetPixel(x, y, fillChar, fillColor);
            }
        }

        private void FillTriangleUp(ScreenBuffer buffer, int centerX, int centerY, int radius, char fillChar, ConsoleColor fillColor, int spacing)
        {
            if (radius < 3) return;

            int sideLength = (int)(radius * Config.Config.SHAPE_TRIANGLE_SIDE_MULTIPLIER);
            int triangleHeight = (int)(sideLength * Config.Config.SHAPE_TRIANGLE_HEIGHT_MULTIPLIER * Config.Config.SHAPE_TRIANGLE_ASPECT_CORRECTION);
            int halfWidth = sideLength / 2;

            int effectiveSpacing = spacing;
            if (radius > 25) effectiveSpacing += 1;
            if (radius > 40) effectiveSpacing += 1;

            for (int y = centerY - triangleHeight; y <= centerY + triangleHeight; y += effectiveSpacing)
            {
                float rowProgress = (float)(y - (centerY - triangleHeight)) / (triangleHeight * 2);
                int rowHalfWidth = (int)(halfWidth * rowProgress);

                for (int x = centerX - rowHalfWidth; x <= centerX + rowHalfWidth; x += effectiveSpacing)
                {
                    buffer.SetPixel(x, y, fillChar, fillColor);
                }
            }
        }

        private void FillTriangleDown(ScreenBuffer buffer, int centerX, int centerY, int radius, char fillChar, ConsoleColor fillColor, int spacing)
        {
            if (radius < 3) return;

            int sideLength = (int)(radius * Config.Config.SHAPE_TRIANGLE_SIDE_MULTIPLIER);
            int triangleHeight = (int)(sideLength * Config.Config.SHAPE_TRIANGLE_HEIGHT_MULTIPLIER * Config.Config.SHAPE_TRIANGLE_ASPECT_CORRECTION);
            int halfWidth = sideLength / 2;

            int effectiveSpacing = spacing;
            if (radius > 25) effectiveSpacing += 1;
            if (radius > 40) effectiveSpacing += 1;

            for (int y = centerY - triangleHeight; y <= centerY + triangleHeight; y += effectiveSpacing)
            {
                float rowProgress = 1f - (float)(y - (centerY - triangleHeight)) / (triangleHeight * 2);
                int rowHalfWidth = (int)(halfWidth * rowProgress);

                for (int x = centerX - rowHalfWidth; x <= centerX + rowHalfWidth; x += effectiveSpacing)
                {
                    buffer.SetPixel(x, y, fillChar, fillColor);
                }
            }
        }
        #endregion

        #region HelperMethods
        private float GetEffectiveMaxSize()
        {
            if (Config.Config.SHAPE_LAYOUT == ShapeLayout.Concentric)
                return Config.Config.SHAPE_MAX_SIZE_PERCENT;

            int count = Config.Config.SHAPE_COUNT;

            //layouts that center a single shape use full size
            if (count == 1)
                return Config.Config.SHAPE_MAX_SIZE_PERCENT;

            //pyramid with 2 shapes uses full size (falls back to single)
            if (Config.Config.SHAPE_LAYOUT == ShapeLayout.Pyramid && count < 3)
                return Config.Config.SHAPE_MAX_SIZE_PERCENT;

            return Config.Config.SHAPE_MAX_SIZE_PERCENT / count;
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
            if (IsCustomColorEnabled)
            {
                ConsoleColor[] colors = Config.Config.SHAPE_CUSTOM_COLORS;
                if (colors != null && colors.Length > 0)
                    return colors[shapeIndex % colors.Length];
            }

            return Config.Config.SHAPE_UNIFORM_COLOR;
        }

        private int CalculatePolygonHalfWidth(int radius, int sides, float rowProgress)
        {
            //polygon width varies with row...widest at center, narrows toward top/bottom
            //for regular polygons, the width at a given row is proportional to cos(angle)
            double angleFromTop = rowProgress * Math.PI;
            double normalizedWidth = Math.Sin(angleFromTop);

            return (int)(radius * normalizedWidth);
        }

        private int GetVerticalPosition(int height, int count, int index, int spacing)
        {
            if (count == 1) return height / 2;

            int totalSpace = height / count;
            return totalSpace * index + totalSpace / 2;
        }

        private int GetHorizontalPosition(int width, int count, int index, int spacing)
        {
            if (count == 1) return width / 2;

            int totalSpace = width / count;
            return totalSpace * index + totalSpace / 2;
        }

        private int[] GetQuadrantIndices(int count)
        {
            //user defined positions
            if (Config.Config.SHAPE_QUADRANT_INDICES != null && Config.Config.SHAPE_QUADRANT_INDICES.Length > 0)
            {
                int[] result = new int[Math.Min(count, Config.Config.SHAPE_QUADRANT_INDICES.Length)];
                Array.Copy(Config.Config.SHAPE_QUADRANT_INDICES, result, result.Length);
                return result;
            }

            //auto, where no custom coordinates are passed in config
            switch (count)
            {
                case 1:
                    Config.Config.SHAPE_QUADRANT_CENTERED = true; //do centered mode
                    return new int[] { 0, 1, 2, 3 };
                case 2:
                    Config.Config.SHAPE_QUADRANT_CENTERED = false;
                    return new int[] { 0, 3 }; //diagonal
                case 3:
                    Config.Config.SHAPE_QUADRANT_CENTERED = false;
                    return new int[] { 1, 2 }; //reverse diagonal
                case 4:
                default:
                    Config.Config.SHAPE_QUADRANT_CENTERED = false;
                    return new int[] { 0, 1, 2, 3 };
            }
        }
        #endregion
    }
}

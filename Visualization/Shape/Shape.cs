using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Config.Settings.Visualizations;
using TERMINAL_FREQUENCY.Core.Rendering;

namespace TERMINAL_FREQUENCY.Visualization.Shape
{
    /// <summary>
    /// Renders configurable geometric shapes that respond to volume levels and audio spikes.
    /// Implements <see cref="IVolumeReactive"/> for continuous size changes and <see cref="ISpikeReactive"/>
    /// for pump effects on beats. Supports multiple shape types (circle, square, diamond, polygon, triangles),
    /// seven layout modes (single, vertical, horizontal, pyramid, quadrant, concentric),
    /// reverse mode, fill mode, custom colors, and smooth lerp transitions.
    /// </summary>
    public class Shape : IVolumeReactive, ISpikeReactive
    {
        private string _name = "SHAPE";
        private int _modeIndex = 2;
        private float _currentSize = 0f;
        private float _targetSize = 0f;
        private float _peakVolume = 0.1f;
        private Settings _settings;
        private const float SIZE_BOOST = 2.0f; //small boost for layouts where shapes tend to get cramped, non modifiable, user can modify many other settings to get bigger shapes

        string IVisualization.Name => _name;
        int IVisualization.ModeIndex => _modeIndex;
        public bool IsReversed { get; set; }
        public bool IsSmoothingEnabled { get; set; }
        public bool IsCustomColorEnabled { get; set; }
        public bool IsCyclingEnabled { get; set; }

        /// <summary>
        /// Initializes the shape visualization with the given settings.
        /// Reads initial state for reverse mode, smooth mode, and custom colors from <see cref="ShapeSettings"/>.
        /// </summary>
        /// <param name="settings">Loaded or runtime settings to be used</param>
        public Shape(Settings settings)
        {
            _settings = settings;
            IsReversed = _settings.ShapeSettings.ReverseMode;
            IsSmoothingEnabled = _settings.ShapeSettings.SmoothMode;
            IsCustomColorEnabled = _settings.ShapeSettings.UseCustomColor;
        }

        #region IVisualization
        /// <summary>
        /// Updates the shape size based on the current volume level.
        /// In normal mode, higher volume increases the size from <see cref="ShapeSettings.MinSizePercent"/>
        /// toward <see cref="ShapeSettings.MaxSizePercent"/>. In reverse mode, higher volume decreases the size.
        /// Uses <see cref="ShapeSettings.LerpFactor"/> for smooth transitions when enabled.
        /// </summary>
        /// <param name="volume">The smoothed audio volume level from <see cref="AudioCapture"/>.</param>
        public void Update(float volume)
        {
            IsReversed = _settings.ShapeSettings.ReverseMode;
            IsSmoothingEnabled = _settings.ShapeSettings.SmoothMode;
            IsCustomColorEnabled = _settings.ShapeSettings.UseCustomColor;

            float maxSize = GetEffectiveMaxSize();
            float minSize = _settings.ShapeSettings.MinSizePercent;
            float scaledVolume;

            if (IsReversed)
            {
                //track peak and normalize for reverse
                if (volume > _peakVolume)
                    _peakVolume = volume;
                _peakVolume *= 0.995f;

                float normalizedVolume = _peakVolume > 0.01f ? Math.Clamp(volume / _peakVolume, 0f, 1f) : 0f;

                if (normalizedVolume < _settings.ShapeSettings.TriggerThreshold * _settings.ShapeSettings.ReverseVolumeSensitivity)
                    normalizedVolume = 0;


                //boost maxSize artificially to look more like raw volume
                maxSize *= 5;

                _targetSize = maxSize - (normalizedVolume * (maxSize - minSize));
            }
            else
            {
                //raw audio volume
                if (volume < _settings.ShapeSettings.TriggerThreshold)
                    volume = 0;

                scaledVolume = volume * _settings.ShapeSettings.VolumeSensitivity;

                _targetSize = minSize + (scaledVolume * (maxSize - minSize));
            }

            _currentSize = IsSmoothingEnabled
                ? _currentSize + (_targetSize - _currentSize) * _settings.ShapeSettings.LerpFactor
                : _targetSize;
        }

        /// <summary>
        /// Triggers a pump effect on audio spikes. Sets the target size to max (or min if reversed)
        /// for an instant visual burst. If smooth mode is off, snaps immediately.
        /// </summary>
        public void OnSpike()
        {
            _targetSize = IsReversed ? _settings.ShapeSettings.MinSizePercent : GetEffectiveMaxSize();
            if (!IsSmoothingEnabled) _currentSize = _targetSize;
        }

        /// <summary>Calls <see cref="OnSpike()"/>. Required by <see cref="ISpikeReactive"/>.</summary>
        public void OnSpike(float intensity) => OnSpike();
        #endregion

        #region LayoutMethods
        /// <summary>
        /// Routes drawing to the appropriate layout method based on <see cref="ShapeSettings.Layout"/>.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        public void Draw(ScreenBuffer buffer)
        {
            switch (_settings.ShapeSettings.Layout)
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

        /// <summary>
        /// Draws a single shape centered in the console window.
        /// If <see cref="ShapeSettings.FillMode"/> is enabled, draws with full-thickness solid fill.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        private void DrawSingle(ScreenBuffer buffer)
        {
            int centerX = buffer.Width / 2;
            int centerY = buffer.Height / 2;
            int maxDimension = Math.Min(buffer.Width, buffer.Height);
            int radius = (int)(maxDimension * _currentSize / 2);

            int thickness = GetEffectiveThickness(1);

            DrawShapeAt(buffer, centerX, centerY, radius, (_settings.ShapeSettings.FillMode) ? radius : thickness, _settings.ShapeSettings.UniformColor);
        }

        /// <summary>
        /// Draws shapes stacked vertically. For count > 1, divides the screen vertically
        /// and centers each shape in its section. Falls back to <see cref="DrawSingle"/> for count of 1.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        private void DrawVertical(ScreenBuffer buffer)
        {
            int count = _settings.ShapeSettings.Count;

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

                DrawShapeAt(buffer, centerX, centerY, radius, (_settings.ShapeSettings.FillMode) ? radius : thickness, _settings.ShapeSettings.UniformColor);

            }

        }

        /// <summary>
        /// Draws shapes arranged horizontally. For count > 1, divides the screen horizontally
        /// and centers each shape in its section. Falls back to <see cref="DrawSingle"/> for count of 1.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        private void DrawHorizontal(ScreenBuffer buffer)
        {
            int count = _settings.ShapeSettings.Count;

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

                DrawShapeAt(buffer, centerX, centerY, radius, (_settings.ShapeSettings.FillMode) ? radius : thickness, _settings.ShapeSettings.UniformColor);
            }
        }

        /// <summary>
        /// Draws shapes in a pyramid arrangement. Count of 3 places one shape on top and two below.
        /// Count >= 4 inverts the pyramid (two on top, one below). Count < 3 falls back to <see cref="DrawSingle"/>.
        /// Row spacing is controlled by <see cref="ShapeSettings.PyramidRowSpacing"/>.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        private void DrawPyramid(ScreenBuffer buffer)
        {
            int count = _settings.ShapeSettings.Count;
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

                int rowSpacing = (int)(buffer.Height * _settings.ShapeSettings.PyramidRowSpacing);

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

                    DrawShapeAt(buffer, centerX, rowY, radius, (_settings.ShapeSettings.FillMode) ? radius : thickness, _settings.ShapeSettings.UniformColor);

                    shapeIndex++;
                }
            }
        }

        /// <summary>
        /// Draws shapes in screen quadrants. Supports 1-4 shapes with automatic placement.
        /// For 1 shape, forces centered mode. For 2 shapes, uses diagonal corners.
        /// For 3 shapes, uses three corners. For 4 shapes, uses all four quadrants or centered cluster.
        /// Custom quadrant indices can be set via <see cref="ShapeSettings.QuadrantIndices"/>.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        private void DrawQuadrant(ScreenBuffer buffer)
        {
            int count = _settings.ShapeSettings.Count;
            int thickness = GetEffectiveThickness(count);
            int[] indices = GetQuadrantIndices(count);
            int actualShapes = indices.Length;
            int maxDimension = Math.Min(buffer.Width, buffer.Height) / (actualShapes == 1 ? 2 : actualShapes);
            int radius = (int)(maxDimension * _currentSize / 2);
            (int x, int y)[] quads;

            if(count == 4) maxDimension = (int)(maxDimension * SIZE_BOOST);
            if (_settings.ShapeSettings.QuadrantCentered && actualShapes == 4)
            {
                //cluster to middle
                int gap = Math.Min(buffer.Width, buffer.Height) / _settings.ShapeSettings.QuadrantGapDivisor;
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
                DrawShapeAt(buffer, quads[index].x, quads[index].y, radius, (_settings.ShapeSettings.FillMode) ? radius : thickness, _settings.ShapeSettings.UniformColor);
            }
        }

        /// <summary>
        /// Draws concentric rings of shapes from a center point. Each ring is a proportionally smaller
        /// copy of the outer shape. <see cref="ShapeSettings.ConcentricLayers"/> controls the number of rings.
        /// <see cref="ShapeSettings.ConcentricPadding"/> controls spacing between rings.
        /// Falls back to <see cref="DrawSingle"/> for a single layer.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        private void DrawConcentric(ScreenBuffer buffer)
        {
            int count = _settings.ShapeSettings.ConcentricLayers;

            if(count == 1)
            {
                DrawSingle(buffer);
                return;
            }

            int centerX = buffer.Width / 2;
            int centerY = buffer.Height / 2;
            
            int padding = _settings.ShapeSettings.ConcentricPadding;
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
                DrawShapeAt(buffer, centerX, centerY, radius, thickness, GetColor(i));
            }
        }
        #endregion

        #region ShapeDrawingMethods
        /// <summary>
        /// Dispatches drawing to the appropriate shape-specific method based on <see cref="ShapeSettings.Type"/>.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        /// <param name="centerX">Calculated center of X axis to draw buffer to.</param>
        /// <param name="centerY">Calculated center of Y axis to draw buffer to.</param>
        /// <param name="radius">Radius/size of shape.</param>
        /// <param name="thickness">Thickness of shape in pixels.</param>
        /// <param name="color">Colour of shape.</param>
        private void DrawShapeAt(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            if (radius <= 0) return;

            switch (_settings.ShapeSettings.Type)
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

        /// <summary>
        /// Draws a circle using radial segments. When filling (thickness >= radius), boosts segment density
        /// and minimum segments for solid appearance. Uses <see cref="ShapeSettings.Character"/> for pixels.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        /// <param name="centerX">Calculated center of X axis to draw buffer to.</param>
        /// <param name="centerY">Calculated center of Y axis to draw buffer to.</param>
        /// <param name="radius">Radius/size of shape.</param>
        /// <param name="thickness">Thickness of shape in pixels.</param>
        /// <param name="color">Colour of shape.</param>
        private void DrawCircle(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            bool isFill = thickness >= radius - 1;
            float density = isFill ? 1.5f : _settings.ShapeSettings.CircleSegmentDensity;
            int minSeg = isFill ? 24 : _settings.ShapeSettings.CircleMinSegments;
            int maxSeg = isFill ?  100 * (int)(_settings.ShapeSettings.MaxSizePercent * 100) : _settings.ShapeSettings.CircleMaxSegments;

            int innerRadius = Math.Max(0, radius - thickness);

            for (int r = innerRadius; r <= radius; r++)
            {
                if (r == 0) continue;

                int segments = (int)(2 * Math.PI * r * density);
                if (segments < minSeg) segments = minSeg;
                if (segments > maxSeg) segments = maxSeg;

                for (int i = 0; i < segments; i++)
                {
                    double angle = (i * 2 * Math.PI) / segments;
                    int x = centerX + (int)(Math.Cos(angle) * r);
                    int y = centerY + (int)(Math.Sin(angle) * r * 0.45);
                    buffer.SetPixel(x, y, _settings.ShapeSettings.Character, color);
                }
            }

            if (radius <= thickness + 1)
            {
                buffer.SetPixel(centerX, centerY, _settings.ShapeSettings.Character, color);
                buffer.SetPixel(centerX + 1, centerY, _settings.ShapeSettings.Character, color);
                buffer.SetPixel(centerX, centerY + 1, _settings.ShapeSettings.Character, color);
                buffer.SetPixel(centerX - 1, centerY, _settings.ShapeSettings.Character, color);
                buffer.SetPixel(centerX, centerY - 1, _settings.ShapeSettings.Character, color);
            }
        }

        /// <summary>
        /// Draws a square (or rectangle) using four edges. When filling (thickness >= radius),
        /// draws solid rows instead of just edges for a completely filled shape.
        /// Supports configurable width and height ratios via <see cref="ShapeSettings.SquareWidthRatio"/>
        /// and <see cref="ShapeSettings.SquareHeightRatio"/>.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        /// <param name="centerX">Calculated center of X axis to draw buffer to.</param>
        /// <param name="centerY">Calculated center of Y axis to draw buffer to.</param>
        /// <param name="radius">Radius/size of shape.</param>
        /// <param name="thickness">Thickness of shape in pixels.</param>
        /// <param name="color">Colour of shape.</param>
        private void DrawSquare(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            int halfWidth = (int)(radius * _settings.ShapeSettings.SquareWidthRatio);
            int halfHeight = (int)(radius * _settings.ShapeSettings.SquareHeightRatio * 0.45f);

            for (int t = 0; t < thickness; t++)
            {
                int top = centerY - halfHeight + t;
                int bottom = centerY + halfHeight - t;
                int left = centerX - halfWidth + t;
                int right = centerX + halfWidth - t;

                // If filling (thick), draw solid rows instead of just edges
                if (thickness >= radius - 1)
                {
                    for (int y = top; y <= bottom; y++)
                        for (int x = left; x <= right; x++)
                            buffer.SetPixel(x, y, _settings.ShapeSettings.Character, color);
                }
                else
                {
                    for (int x = left; x <= right; x++)
                        buffer.SetPixel(x, top, _settings.ShapeSettings.Character, color);
                    for (int x = left; x <= right; x++)
                        buffer.SetPixel(x, bottom, _settings.ShapeSettings.Character, color);
                    for (int y = top; y <= bottom; y++)
                        buffer.SetPixel(left, y, _settings.ShapeSettings.Character, color);
                    for (int y = top; y <= bottom; y++)
                        buffer.SetPixel(right, y, _settings.ShapeSettings.Character, color);
                }
            }
        }

        /// <summary>
        /// Draws a diamond (rotated square). When filling, uses a scanline approach row-by-row
        /// for solid fill. In outline mode, draws four edges using Bresenham's line algorithm.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        /// <param name="centerX">Calculated center of X axis to draw buffer to.</param>
        /// <param name="centerY">Calculated center of Y axis to draw buffer to.</param>
        /// <param name="radius">Radius/size of shape.</param>
        /// <param name="thickness">Thickness of shape in pixels.</param>
        /// <param name="color">Colour of shape.</param>
        private void DrawDiamond(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            int halfWidth = radius;
            int halfHeight = (int)(radius * 0.45f);

            // Fill mode: use scanline approach
            if (thickness >= radius - 1)
            {
                for (int y = centerY - halfHeight; y <= centerY + halfHeight; y++)
                {
                    float rowProgress = Math.Abs(y - centerY) / (float)halfHeight;
                    int rowHalfWidth = (int)(halfWidth * (1 - rowProgress));
                    for (int x = centerX - rowHalfWidth; x <= centerX + rowHalfWidth; x++)
                        buffer.SetPixel(x, y, _settings.ShapeSettings.Character, color);
                }
                return;
            }

            // Outline mode: draw edges
            for (int t = 0; t < thickness; t++)
            {
                int hw = halfWidth - t;
                int hh = halfHeight - t;
                DrawLine(buffer, centerX, centerY - hh, centerX + hw, centerY, color);
                DrawLine(buffer, centerX + hw, centerY, centerX, centerY + hh, color);
                DrawLine(buffer, centerX, centerY + hh, centerX - hw, centerY, color);
                DrawLine(buffer, centerX - hw, centerY, centerX, centerY - hh, color);
            }
        }

        /// <summary>
        /// Draws a line from (x0,y0) to (x1,y1) using Bresenham's line algorithm.
        /// Produces clean diagonal lines for diamond and polygon shapes.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        /// <param name="x0">Starting X position of the line</param>
        /// <param name="y0">Starting Y position of the line</param>
        /// <param name="x1">Ending X position of the line</param>
        /// <param name="y1">Ending Y position of the line</param>
        /// <param name="color">Colour of the line</param>
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
                buffer.SetPixel(x0, y0, _settings.ShapeSettings.Character, color);

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

        /// <summary>
        /// Draws a regular polygon with the number of sides specified by <see cref="ShapeSettings.PolygonSides"/>.
        /// When filling, increases the side count for a smoother shape. Vertices are calculated on a circle
        /// then connected with lines. An offset of -PI/2 ensures a flat top edge.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        /// <param name="centerX">Calculated center of X axis to draw buffer to.</param>
        /// <param name="centerY">Calculated center of Y axis to draw buffer to.</param>
        /// <param name="radius">Radius/size of shape.</param>
        /// <param name="thickness">Thickness of shape in pixels.</param>
        /// <param name="color">Colour of shape.</param>
        private void DrawPolygon(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            bool isFill = thickness >= radius - 1;
            int sides = isFill ? Math.Max(_settings.ShapeSettings.PolygonSides, 16) : _settings.ShapeSettings.PolygonSides;

            (int x, int y)[] vertices = new (int, int)[sides];
            for (int i = 0; i < sides; i++)
            {
                double angle = (i * 2 * Math.PI) / sides - Math.PI / 2;
                vertices[i].x = centerX + (int)(Math.Cos(angle) * radius);
                vertices[i].y = centerY + (int)(Math.Sin(angle) * radius * 0.45f);
            }

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
            }
        }

        /// <summary>
        /// Draws an upward-pointing triangle. Side length and proportions controlled by
        /// <see cref="ShapeSettings.TriangleSideMultiplier"/>, <see cref="ShapeSettings.TriangleHeightMultiplier"/>,
        /// and <see cref="ShapeSettings.TriangleAspectCorrection"/>.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        /// <param name="centerX">Calculated center of X axis to draw buffer to.</param>
        /// <param name="centerY">Calculated center of Y axis to draw buffer to.</param>
        /// <param name="radius">Radius/size of shape.</param>
        /// <param name="thickness">Thickness of shape in pixels.</param>
        /// <param name="color">Colour of shape.</param>
        private void DrawTriangleUp(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            int sideLength = (int)(radius * _settings.ShapeSettings.TriangleSideMultiplier);
            int triangleHeight = (int)(sideLength * _settings.ShapeSettings.TriangleHeightMultiplier * _settings.ShapeSettings.TriangleAspectCorrection);

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

        /// <summary>
        /// Draws a downward-pointing triangle. Uses the same proportion settings as <see cref="DrawTriangleUp"/>.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        /// <param name="centerX">Calculated center of X axis to draw buffer to.</param>
        /// <param name="centerY">Calculated center of Y axis to draw buffer to.</param>
        /// <param name="radius">Radius/size of shape.</param>
        /// <param name="thickness">Thickness of shape in pixels.</param>
        /// <param name="color">Colour of shape.</param>
        private void DrawTriangleDown(ScreenBuffer buffer, int centerX, int centerY, int radius, int thickness, ConsoleColor color)
        {
            int sideLength = (int)(radius * _settings.ShapeSettings.TriangleSideMultiplier);
            int triangleHeight = (int)(sideLength * _settings.ShapeSettings.TriangleHeightMultiplier * _settings.ShapeSettings.TriangleAspectCorrection);

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

        #region HelperMethods
        /// <summary>
        /// Returns the effective maximum size based on layout and shape count.
        /// Concentric layout always uses full <see cref="ShapeSettings.MaxSizePercent"/>.
        /// Single shapes use full size. Multi-shape layouts divide the max by the shape count.
        /// </summary>
        private float GetEffectiveMaxSize()
        {
            if (_settings.ShapeSettings.Layout == ShapeLayout.Concentric)
                return _settings.ShapeSettings.MaxSizePercent;

            int count = _settings.ShapeSettings.Count;

            //layouts that center a single shape use full size
            if (count == 1)
                return _settings.ShapeSettings.MaxSizePercent;

            //pyramid with 2 shapes uses full size (falls back to single)
            if (_settings.ShapeSettings.Layout == ShapeLayout.Pyramid && count < 3)
                return _settings.ShapeSettings.MaxSizePercent;

            return _settings.ShapeSettings.MaxSizePercent / count;
        }

        /// <summary>
        /// Calculates effective border thickness scaled by the number of shapes on screen.
        /// More shapes reduce the thickness to prevent overcrowding. Clamped by <see cref="ShapeSettings.ThicknessMax"/>.
        /// </summary>
        /// <param name="shapeCount">How many shapes are in the window to determine effective thickness</param>
        private int GetEffectiveThickness(int shapeCount)
        {
            int thickness = _settings.ShapeSettings.Thickness;
            int maxThickness = _settings.ShapeSettings.ThicknessMax;

            thickness = Math.Max(1, thickness / shapeCount);
            return Math.Min(thickness, maxThickness);
        }

        /// <summary>
        /// Returns the color for a shape at the given index. If <see cref="IsCustomColorEnabled"/>,
        /// picks from <see cref="ShapeSettings.CustomColors"/> by index (wrapping as needed).
        /// Otherwise returns <see cref="ShapeSettings.UniformColor"/>.
        /// </summary>
        /// <param name="shapeIndex">The index of the shape within the current layout (0-based).</param>
        /// <returns>The <see cref="ConsoleColor"/> for this shape based on custom or uniform color settings.</returns>
        private ConsoleColor GetColor(int shapeIndex)
        {
            if (IsCustomColorEnabled)
            {
                ConsoleColor[] colors = _settings.ShapeSettings.CustomColors;
                if (colors != null && colors.Length > 0)
                    return colors[shapeIndex % colors.Length];
            }

            return _settings.ShapeSettings.UniformColor;
        }

        /// <summary>
        /// Calculates the half-width of a regular polygon at a given row position.
        /// Uses sine to determine how far the polygon edge extends from center at each row.
        /// </summary>
        /// <param name="radius">The radius of the polygon.</param>
        /// <param name="sides">Number of sides of the polygon.</param>
        /// <param name="rowProgress">Progress through the polygon from top (0) to center (0.5) to bottom (1).</param>
        private int CalculatePolygonHalfWidth(int radius, int sides, float rowProgress)
        {
            //polygon width varies with row...widest at center, narrows toward top/bottom
            //for regular polygons, the width at a given row is proportional to cos(angle)
            double angleFromTop = rowProgress * Math.PI;
            double normalizedWidth = Math.Sin(angleFromTop);

            return (int)(radius * normalizedWidth);
        }

        /// <summary>
        /// Returns the Y position for a shape in a vertical layout.
        /// Evenly divides the screen height among the shape count and positions each shape
        /// at the center of its allocated space.
        /// </summary>
        /// <param name="height">Total console height in characters.</param>
        /// <param name="count">Number of shapes in the layout.</param>
        /// <param name="index">Index of this shape (0-based).</param>
        /// <param name="spacing">Spacing between shapes in characters.</param>
        private int GetVerticalPosition(int height, int count, int index, int spacing)
        {
            if (count == 1) return height / 2;

            int totalSpace = height / count;
            return totalSpace * index + totalSpace / 2;
        }

        /// <summary>
        /// Returns the X position for a shape in a horizontal layout.
        /// Evenly divides the screen width among the shape count and positions each shape
        /// at the center of its allocated space.
        /// </summary>
        /// <param name="width">Total console width in characters.</param>
        /// <param name="count">Number of shapes in the layout.</param>
        /// <param name="index">Index of this shape (0-based).</param>
        /// <param name="spacing">Spacing between shapes in characters.</param>
        private int GetHorizontalPosition(int width, int count, int index, int spacing)
        {
            if (count == 1) return width / 2;

            int totalSpace = width / count;
            return totalSpace * index + totalSpace / 2;
        }

        /// <summary>
        /// Determines which quadrant indices to use based on shape count.
        /// Supports user-defined indices from <see cref="ShapeSettings.QuadrantIndices"/>
        /// or automatic placement: 1=centered, 2=diagonal, 3=three corners, 4=all quadrants.
        /// </summary>
        /// <param name="count">The total number of shapes to place in quadrants.</param>
        private int[] GetQuadrantIndices(int count)
        {
            //user defined positions
            if (_settings.ShapeSettings.QuadrantIndices != null && _settings.ShapeSettings.QuadrantIndices.Length > 0)
            {
                int[] result = new int[Math.Min(count, _settings.ShapeSettings.QuadrantIndices.Length)];
                Array.Copy(_settings.ShapeSettings.QuadrantIndices, result, result.Length);
                return result;
            }

            //auto, where no custom coordinates are passed in config
            switch (count)
            {
                case 1:
                    _settings.ShapeSettings.QuadrantCentered = true; //do centered mode
                    return new int[] { 0, 1, 2, 3 };
                case 2:
                    _settings.ShapeSettings.QuadrantCentered = false;
                    return new int[] { 0, 3 }; //diagonal
                case 3:
                    _settings.ShapeSettings.QuadrantCentered = false;
                    return new int[] { 1, 2 }; //reverse diagonal
                case 4:
                default:
                    _settings.ShapeSettings.QuadrantCentered = false;
                    return new int[] { 0, 1, 2, 3 };
            }
        }
        #endregion
    }
}

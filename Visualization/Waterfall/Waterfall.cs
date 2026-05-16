using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Config.Settings.Visualizations;
using TERMINAL_FREQUENCY.Core.Rendering;

namespace TERMINAL_FREQUENCY.Visualization.Waterfall
{
    /// <summary>
    /// Renders waterfall streams that flow across the console in response to volume spikes.
    /// Implements <see cref="IVolumeReactive"/> to update existing streams and
    /// <see cref="ISpikeReactive"/> to spawn new streams on audio beats.
    /// </summary>
    public class Waterfall : IVolumeReactive, ISpikeReactive
    {
        private Settings _settings;
        private string _name = "WATERFALL";
        private int _modeIndex = 1;
        private List<WaterfallStream> _streams = new List<WaterfallStream>();
        private readonly object _streamLock = new object();
        private int _clockwiseIndex = 0;
        private bool _topBottomToggle = true; // true = top, false = bottom
        private bool _leftRightToggle = true; // true = left, false = right
        private static readonly VisualizationOrigin[] _clockwiseOrder = { VisualizationOrigin.Top, VisualizationOrigin.Right, VisualizationOrigin.Bottom, VisualizationOrigin.Left };
        private static readonly VisualizationOrigin[] _antiClockwiseOrder = { VisualizationOrigin.Top, VisualizationOrigin.Left, VisualizationOrigin.Bottom, VisualizationOrigin.Right };

        string IVisualization.Name => _name;
        int IVisualization.ModeIndex => _modeIndex;

        /// <summary>
        /// Returns the current number of active waterfall streams. Thread-safe.
        /// </summary>
        public int StreamCount
        {
            get
            {
                lock (_streamLock)
                {
                    return _streams.Count;
                }
            }
        }

        /// <summary>
        /// Initializes the waterfall with the given settings. Sets the starting clockwise index
        /// based on the configured <see cref="WaterfallSettings.Origin"/>.
        /// </summary>
        public Waterfall(Settings settings)
        {
            _settings = settings;
            _clockwiseIndex = Array.IndexOf(_clockwiseOrder, _settings.Waterfall.Origin);
            if (_clockwiseIndex < 0) _clockwiseIndex = 0;
        }

        /// <summary>
        /// Updates all active streams each frame. Advances their progress and removes any
        /// that have expired. Called every frame by the audio capture.
        /// </summary>
        public void Update(float volume)
        {
            lock (_streamLock)
            {
                for (int i = _streams.Count - 1; i >= 0; i--)
                {
                    _streams[i].Update();
                    if (!_streams[i].IsAlive)
                        _streams.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Spawns new waterfall streams in response to a volume spike.
        /// Respects <see cref="WaterfallSettings.OnlySpawnOnThreshold"/> and
        /// <see cref="WaterfallSettings.TriggerThreshold"/>. Creates multiple offset streams
        /// based on <see cref="WaterfallSettings.Thickness"/> for a thicker visual effect.
        /// In <see cref="WaterfallMode.All"/> mode, spawns streams from all four edges simultaneously.
        /// </summary>
        /// <param name="intensity">The volume intensity of the spike that triggered this event.</param>
        public void OnSpike(float intensity)
        {
            if (_settings.Waterfall.OnlySpawnOnThreshold && intensity < _settings.Waterfall.TriggerThreshold) return;
            int thickness = _settings.Waterfall.Thickness;
            int halfThick = (thickness - 1) / 2;

            lock (_streamLock)
            {
                if (_settings.Waterfall.Mode == WaterfallMode.All)
                {
                    VisualizationOrigin[] allOrigins =
                    {
                        VisualizationOrigin.Top,
                        VisualizationOrigin.Bottom,
                        VisualizationOrigin.Left,
                        VisualizationOrigin.Right
                    };

                    foreach (VisualizationOrigin o in allOrigins)
                            for (int t = -halfThick; t <= halfThick; t++)
                                AddStream(intensity, o, t);
                }
                else
                {
                    VisualizationOrigin origin = GetNextOrigin();
                    for (int t = -halfThick; t <= halfThick; t++)
                        AddStream(intensity, origin, t);
                }
            }

            // Update all _streams
            lock (_streamLock)
            {
                for (int i = _streams.Count - 1; i >= 0; i--)
                {
                    _streams[i].Update();
                    if (!_streams[i].IsAlive)
                        _streams.RemoveAt(i);
                }
            }
        }

        /// <summary>Calls <see cref="OnSpike(float)"/> with zero intensity. Required by <see cref="ISpikeReactive"/>.</summary>
        public void OnSpike() => OnSpike(0f);

        /// <summary>
        /// Draws all active waterfall streams to the console buffer.
        /// Creates a thread-safe copy of the stream list before iterating.
        /// In debug mode, displays the current stream count.
        /// </summary>
        public void Draw(ScreenBuffer buffer)
        {
            List<WaterfallStream> streamsCopy;
            lock(_streamLock)
            {
                streamsCopy = new List<WaterfallStream>(_streams);
            }

            if (_settings.Global.EnableDebugMode)
            {
                buffer.DrawString(0, 0, $"Streams: {streamsCopy.Count}", ConsoleColor.Yellow);
            }

            foreach (WaterfallStream stream in streamsCopy)
                DrawStream(buffer, stream);
        }

        /// <summary>
        /// Determines the origin for the next stream based on the current <see cref="WaterfallMode"/>.
        /// Normal mode always returns the configured origin. Clockwise and anti-clockwise rotate
        /// through origins in order. TopBottom and LeftRight alternate between two origins.
        /// All mode returns a random origin.
        /// </summary>
        private VisualizationOrigin GetNextOrigin()
        {
            switch (_settings.Waterfall.Mode)
            {
                case WaterfallMode.Normal:
                    return _settings.Waterfall.Origin;

                case WaterfallMode.Clockwise:
                    int startIdx = Array.IndexOf(_clockwiseOrder, _settings.Waterfall.Origin);
                    VisualizationOrigin next = _clockwiseOrder[(startIdx + _clockwiseIndex) % 4];
                    _clockwiseIndex = (_clockwiseIndex + 1) % 4;
                    return next;

                case WaterfallMode.AntiClockwise:
                    startIdx = Array.IndexOf(_antiClockwiseOrder, _settings.Waterfall.Origin);
                    next = _antiClockwiseOrder[(startIdx + _clockwiseIndex) % 4];
                    _clockwiseIndex = (_clockwiseIndex + 1) % 4;
                    return next;

                case WaterfallMode.TopBottom:
                    _topBottomToggle = !_topBottomToggle;
                    return _topBottomToggle ? VisualizationOrigin.Top : VisualizationOrigin.Bottom;

                case WaterfallMode.LeftRight:
                    _leftRightToggle = !_leftRightToggle;
                    return _leftRightToggle ? VisualizationOrigin.Left : VisualizationOrigin.Right;
                case WaterfallMode.All:
                    return (VisualizationOrigin)new Random().Next(4); // 0=Top, 1=Bottom, 2=Left, 3=Right

                default:
                    return _settings.Waterfall.Origin;
            }
        }

        /// <summary>
        /// Routes a stream to the correct drawing method based on its <see cref="WaterfallStream.Origin"/>.
        /// Top and Center draw from top downward. Bottom draws from bottom upward.
        /// Left draws from left to right. Right draws from right to left.
        /// </summary>
        private void DrawStream(ScreenBuffer buffer, WaterfallStream stream)
        {
            switch(stream.Origin)
            {
                case VisualizationOrigin.Top:
                case VisualizationOrigin.Center:
                    DrawVerticalStream(buffer, stream, fromTop: true);
                    break;
                case VisualizationOrigin.Bottom:
                    DrawVerticalStream(buffer, stream, fromTop: false);
                    break;
                case VisualizationOrigin.Left:
                    DrawHorizontalStream(buffer, stream, fromLeft: true);
                    break;
                case VisualizationOrigin.Right:
                    DrawHorizontalStream(buffer, stream, fromLeft: false);
                    break;
            }
        }

        /// <summary>
        /// Draws a waterfall stream flowing vertically (top-to-bottom or bottom-to-top).
        /// Calculates the stream's Y position based on progress, then draws characters
        /// along a horizontal line with a trailing curve effect behind the main flow.
        /// Applies <see cref="WaterfallStream.ThicknessOffset"/> for multi-stream thickness.
        /// </summary>
        private void DrawVerticalStream(ScreenBuffer buffer, WaterfallStream stream, bool fromTop)
        {
            int consoleHeight = buffer.Height;
            int consoleWidth = buffer.Width;

            int streamY;
            if (stream.IsReversed)
            {
                int centerY = consoleHeight / 2;
                if (fromTop)
                    streamY = centerY - (int)(stream.Progress * centerY);
                else
                    streamY = centerY + (int)(stream.Progress * (consoleHeight - centerY));
            }
            else
            {
                if (fromTop)
                    streamY = (int)(stream.Progress * consoleHeight);
                else
                    streamY = consoleHeight - 1 - (int)(stream.Progress * consoleHeight);
            }

            int halfWidth = (int)(stream.GetWidth(consoleWidth) / 2);
            int centerX = consoleWidth / 2;

            int startX = Math.Max(0, centerX - halfWidth);
            int endX = Math.Min(consoleWidth - 1, centerX + halfWidth);

            ConsoleColor color = stream.GetColor();

            for (int x = startX; x <= endX; x++)
            {
                int positionInStream = x - startX;
                int totalPositions = endX - startX + 1;
                char c = stream.GetCharacter(positionInStream, totalPositions);

                if (c != ' ')
                {
                    int drawY = streamY + stream.ThicknessOffset;
                    if (drawY >= 0 && drawY < consoleHeight)
                        buffer.SetPixel(x, drawY, c, color);
                }
            }

            int curveOffset = (int)(halfWidth * _settings.Waterfall.CurveIntensityVertical);
            if (curveOffset > 0 && streamY > 0 && streamY < consoleHeight - 1)
            {
                int curveY = fromTop ? streamY - 1 : streamY + 1;
                if (curveY >= 0 && curveY < consoleHeight)
                {
                    for (int x = centerX - curveOffset; x <= centerX + curveOffset; x++)
                    {
                        int drawY = curveY + stream.ThicknessOffset;
                        if (drawY >= 0 && drawY < consoleHeight)
                            buffer.SetPixel(x, drawY, _settings.Waterfall.CurveChar, color);
                    }
                }
            }
        }

        /// <summary>
        /// Draws a waterfall stream flowing horizontally (left-to-right or right-to-left).
        /// Functions identically to <see cref="DrawVerticalStream"/> but operates on the X axis.
        /// Characters are converted from vertical to horizontal equivalents using
        /// <see cref="WaterfallSettings.HorizontalChars"/>.
        /// </summary>
        private void DrawHorizontalStream(ScreenBuffer buffer, WaterfallStream stream, bool fromLeft)
        {
            int consoleHeight = buffer.Height;
            int consoleWidth = buffer.Width;

            int streamX;
            if (stream.IsReversed)
            {
                int centerX = consoleWidth / 2;
                if (fromLeft)
                    streamX = centerX - (int)(stream.Progress * centerX);
                else
                    streamX = centerX + (int)(stream.Progress * (consoleWidth - centerX));
            }
            else
            {
                if (fromLeft)
                    streamX = (int)(stream.Progress * consoleWidth);
                else
                    streamX = consoleWidth - 1 - (int)(stream.Progress * consoleWidth);
            }

            int halfHeight = (int)(stream.GetWidth(consoleHeight) / 2);
            int centerY = consoleHeight / 2;

            int startY = Math.Max(0, centerY - halfHeight);
            int endY = Math.Min(consoleHeight - 1, centerY + halfHeight);

            ConsoleColor color = stream.GetColor();

            for (int y = startY; y <= endY; y++)
            {
                int positionInStream = y - startY;
                int totalPositions = endY - startY + 1;
                char c = stream.GetCharacter(positionInStream, totalPositions);

                if (c != ' ')
                {
                    if (c == _settings.Waterfall.VerticalChars[0]) c = _settings.Waterfall.HorizontalChars[0];
                    else if (c == _settings.Waterfall.VerticalChars[1]) c = _settings.Waterfall.HorizontalChars[1];
                    else if (c == _settings.Waterfall.VerticalChars[2]) c = _settings.Waterfall.HorizontalChars[2];

                    int drawX = streamX + stream.ThicknessOffset;
                    if (drawX >= 0 && drawX < consoleWidth)
                        buffer.SetPixel(drawX, y, c, color);
                }
            }

            int curveOffset = (int)(halfHeight * _settings.Waterfall.CurveIntensityHorizontal);
            if (curveOffset > 0 && streamX > 0 && streamX < consoleWidth - 1)
            {
                int curveX = fromLeft ? streamX - 1 : streamX + 1;
                if (curveX >= 0 && curveX < consoleWidth)
                {
                    for (int y = centerY - curveOffset; y <= centerY + curveOffset; y++)
                    {
                        int drawX = curveX + stream.ThicknessOffset;
                        if (drawX >= 0 && drawX < consoleWidth)
                            buffer.SetPixel(drawX, y, _settings.Waterfall.CurveChar, color);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a new stream to the active list. If the stream count exceeds the maximum,
        /// the oldest streams are removed first. In <see cref="WaterfallMode.All"/> mode,
        /// the maximum is multiplied by <see cref="WaterfallSettings.Thickness"/> to
        /// accommodate streams from all four origins simultaneously.
        /// </summary>
        /// <param name="intensity">The volume intensity that spawned this stream.</param>
        /// <param name="origin">The screen edge this stream originates from.</param>
        /// <param name="offset">The thickness offset for multi-stream rendering.</param>
        private void AddStream(float intensity, VisualizationOrigin origin, int offset)
        {
            int effectiveMax = _settings.Waterfall.Mode == WaterfallMode.All
                ? _settings.Waterfall.MaxStreams * _settings.Waterfall.Thickness
                : _settings.Waterfall.MaxStreams;

            while (_streams.Count >= effectiveMax)
                _streams.RemoveAt(0);

            var s = new WaterfallStream(_settings, intensity, origin, _settings.Waterfall.ReverseMode);
            s.ThicknessOffset = offset;
            _streams.Add(s);
        }
    }
}
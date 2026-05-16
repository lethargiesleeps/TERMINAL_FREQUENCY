using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Config.Settings.Visualizations;
using TERMINAL_FREQUENCY.Core.Rendering;

namespace TERMINAL_FREQUENCY.Visualization.Rings
{
    /// <summary>
    /// Renders expanding and fading rings that respond to volume spikes and continuous volume levels.
    /// Implements <see cref="IVolumeReactive"/> for ambient ring pulsing and <see cref="ISpikeReactive"/>
    /// for spawning new rings on audio beats. Supports reverse mode where rings shrink inward,
    /// multiple color modes, character randomization, and a configurable crosshair with ambient circle.
    /// </summary>
    public class Rings : IVolumeReactive, ISpikeReactive
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

        /// <summary>
        /// Returns the current number of active rings. Thread-safe.
        /// </summary>
        public int RingCount
        {
            get
            {
                lock (_ringLock)
                {
                    return _rings.Count;
                }
            }
        }

        /// <summary>
        /// Initializes the rings visualization with the given settings.
        /// Sets the maximum ring count from <see cref="RingsSettings.MaxRings"/>.
        /// </summary>
        /// <param name="settings">The application settings containing ring configuration.</param>
        public Rings(Settings settings)
        {
            _settings = settings;
            _maxRings = _settings.RingsSettings.MaxRings;
        }

        /// <summary>
        /// Updates all active rings each frame. Advances their radius and reduces their life.
        /// Removes rings that have expired. The smoothed volume is stored for ambient circle drawing.
        /// </summary>
        /// <param name="volume">The smoothed audio volume level from <see cref="AudioCapture"/>.</param>
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

        /// <summary>
        /// Spawns a new ring on each audio spike. If the maximum ring count is reached,
        /// the oldest ring is removed first. Each ring starts at <see cref="RingsSettings.Radius"/>
        /// and expands outward (or shrinks if reverse mode is enabled).
        /// </summary>
        public void OnSpike()
        {
            lock (_ringLock)
            {
                if (_rings.Count >= _maxRings)
                    _rings.RemoveAt(0);
                _rings.Add(new Ring(_settings));
            }
        }

        /// <summary>Calls <see cref="OnSpike()"/>. Required by <see cref="ISpikeReactive"/>.</summary>
        /// <param name="intensity">The volume intensity of the spike. Unused by rings.</param>
        public void OnSpike(float intensity) => OnSpike();

        /// <summary>
        /// Draws all active rings and the ambient circle to the console buffer.
        /// Creates a thread-safe copy of the ring list before iterating.
        /// The ambient circle pulses with <see cref="_smoothedVolume"/> and is drawn
        /// as a dotted ring around the center point. Each ring is drawn as a segmented circle
        /// with characters and colors determined by its lifecycle stage.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
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
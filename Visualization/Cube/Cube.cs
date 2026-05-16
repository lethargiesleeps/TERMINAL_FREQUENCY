using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Config.Settings.Audio;
using TERMINAL_FREQUENCY.Config.Settings.Visualizations;
using TERMINAL_FREQUENCY.Core.Rendering;

namespace TERMINAL_FREQUENCY.Visualization.Cube
{
    /// <summary>
    /// Renders a 3D rotating ASCII cube with z-buffering for proper face occlusion.
    /// Implements <see cref="IVolumeReactive"/> for volume-triggered rotation and pulsing,
    /// and <see cref="IFrequencyReactive"/> for frequency-triggered rotation based on bass energy.
    /// Each of the six faces can have a unique character, and rotation can be frozen per axis.
    /// Supports continuous spinning, audio-reactive stepping, and a pulse effect that expands the cube with volume.
    /// Cube code inspired by Chuehan Kuo (https://hackmd.io/@ChuehanKuo/rysEQMpyeg). Converted to C# by me and coded to use this project's renderer buffers/
    /// </summary>
    public class Cube : IVolumeReactive, IFrequencyReactive
    {
        private Settings _settings;
        private string _name = "CUBE";
        private int _modeIndex = 4;
        private float _angleA, _angleB, _angleC;
        private float[] _zBuffer;
        private float _pulseAmount = 0f;
        private Random _rnd = new Random();

        string IVisualization.Name => _name;
        int IVisualization.ModeIndex => _modeIndex;

        /// <summary>
        /// Initializes the cube visualization. The z-buffer is allocated at minimum size
        /// and resized in <see cref="Draw"/> to match the current console dimensions.
        /// </summary>
        /// <param name="settings">Application settings containing cube configuration.</param>
        public Cube(Settings settings)
        {
            _settings = settings;
            _zBuffer = new float[1];
        }

        /// <summary>
        /// Draws the 3D cube to the console buffer. Clears and resizes the z-buffer each frame.
        /// In <see cref="CubeRotationMode.Continuous"/> mode, applies rotation every frame.
        /// Renders all six faces with per-pixel z-buffering for correct occlusion.
        /// The pulse effect is applied to the zoom level during projection.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        public void Draw(ScreenBuffer buffer)
        {

            int width = buffer.Width;
            int height = buffer.Height;

            if (_zBuffer.Length != width * height)
                _zBuffer = new float[width * height];

            Array.Fill(_zBuffer, 0f);

            if (_settings.Cube.RotationMode == CubeRotationMode.Continuous)
                ApplyRotation();

            // Render cube faces
            float cubeHalfSize = _settings.Cube.CubeWidth;
            float pointSpacing = _settings.Cube.PointDensity;
            char[] chars = _settings.Cube.FaceCharacters;

            for (float x = -cubeHalfSize; x < cubeHalfSize; x += pointSpacing)
            {
                for (float y = -cubeHalfSize; y < cubeHalfSize; y += pointSpacing)
                {
                    RenderFace(buffer, width, height, x, y, -cubeHalfSize, chars[0], _settings.Cube.Color); //front
                    RenderFace(buffer, width, height, cubeHalfSize, y, x, chars[1], _settings.Cube.Color);   //right
                    RenderFace(buffer, width, height, -cubeHalfSize, y, -x, chars[2], _settings.Cube.Color);  //left
                    RenderFace(buffer, width, height, -x, y, cubeHalfSize, chars[3], _settings.Cube.Color);   //back
                    RenderFace(buffer, width, height, x, -cubeHalfSize, -y, chars[4], _settings.Cube.Color);  //bottom
                    RenderFace(buffer, width, height, x, cubeHalfSize, y, chars[5], _settings.Cube.Color);    //top
                }
            }
        }

        /// <summary>
        /// Checks frequency data for bass energy and triggers rotation when above threshold.
        /// If <see cref="FftSettings.DedicatedBassBand"/> is enabled, uses band 0 directly.
        /// Otherwise averages the first quarter of all frequency bands for a bass estimate.
        /// Only active when <see cref="CubeSettings.RotationMode"/> is OnFrequency.
        /// </summary>
        /// <param name="bands">Normalized frequency band data from the FFT analyzer.</param>
        public void OnFrequencyData(float[] bands)
        {
            if (_settings.Cube.RotationMode == CubeRotationMode.OnFrequency)
            {
                if (bands == null || bands.Length == 0) return;

                float bassEnergy;
                if (_settings.Fft.DedicatedBassBand)
                {
                    bassEnergy = bands[0];
                }
                else
                {
                    int bassBandCount = Math.Max(1, bands.Length / 4);
                    bassEnergy = 0;
                    for (int i = 0; i < bassBandCount; i++)
                        bassEnergy += bands[i];
                    bassEnergy /= bassBandCount;
                }

                if (bassEnergy > _settings.Cube.FrequencyThreshold)
                    ApplyRotation();
            }
        }

        /// <summary>
        /// Updates the cube based on continuous volume level. Triggers rotation when volume
        /// exceeds <see cref="CubeSettings.VolumeThreshold"/> in OnVolume mode.
        /// Updates the pulse amount based on volume when <see cref="CubeSettings.PulseEnabled"/> is true.
        /// When pulse is disabled, the pulse amount decays back to zero.
        /// </summary>
        /// <param name="volume">The smoothed audio volume level from <see cref="AudioCapture"/>.</param>
        public void Update(float volume)
        {
            if (_settings.Cube.RotationMode == CubeRotationMode.OnVolume && volume > _settings.Cube.VolumeThreshold)
                ApplyRotation();

            if (_settings.Cube.PulseEnabled)
                _pulseAmount = volume * _settings.Cube.PulseIntensity;
            else
                _pulseAmount *= _settings.Cube.PulseDecay;

        }

        /// <summary>
        /// Projects and draws a single point on one cube face. Applies the 3D rotation matrix,
        /// translates by camera distance, projects to 2D screen coordinates with zoom and pulse,
        /// and performs z-buffer testing for occlusion. Points behind the camera are skipped.
        /// </summary>
        /// <param name="buffer">The screen buffer to draw to.</param>
        /// <param name="w">Console width in characters.</param>
        /// <param name="h">Console height in characters.</param>
        /// <param name="x">Local X coordinate relative to cube center.</param>
        /// <param name="y">Local Y coordinate relative to cube center.</param>
        /// <param name="z">Local Z coordinate relative to cube center.</param>
        /// <param name="c">Character to draw for this face point.</param>
        /// <param name="color">Console color for this face point.</param>
        private void RenderFace(ScreenBuffer buffer, int w, int h, float x, float y, float z, char c, ConsoleColor color)
        {
            float xPos = CalculateX(x, y, z);
            float yPos = CalculateY(x, y, z);
            float zPos = CalculateZ(x, y, z) + _settings.Cube.DistanceFromCam;

            if (zPos <= 0) return;

            float oneOverZ = 1f / zPos;
            float pulsedZoom = _settings.Cube.ZoomLevel * (1f + _pulseAmount);
            int projectedScreenX = (int)(w / 2 + pulsedZoom * oneOverZ * xPos * 2);
            int projectedScreenY = (int)(h / 2 + pulsedZoom * oneOverZ * yPos);

            if (projectedScreenX < 0 || projectedScreenX >= w || projectedScreenY < 0 || projectedScreenY >= h) return;

            int index = projectedScreenY * w + projectedScreenX;
            if (oneOverZ > _zBuffer[index])
            {
                _zBuffer[index] = oneOverZ;
                buffer.SetPixel(projectedScreenX, projectedScreenY, c, color);
            }
        }

        /// <summary>
        /// Rotates a 3D point around all three axes and returns the transformed X coordinate.
        /// Uses the combined rotation matrix derived from the current angles A, B, and C.
        /// </summary>
        /// <param name="localX">Original X coordinate relative to cube center.</param>
        /// <param name="localY">Original Y coordinate relative to cube center.</param>
        /// <param name="localZ">Original Z coordinate relative to cube center.</param>
        /// <returns>The rotated X coordinate.</returns>
        private float CalculateX(float localX, float localY, float localZ)
        {
            return localX * MathF.Cos(_angleC) * MathF.Cos(_angleB)
                 + localY * MathF.Cos(_angleC) * MathF.Sin(_angleB) * MathF.Sin(_angleA)
                 - localY * MathF.Sin(_angleC) * MathF.Cos(_angleA)
                 + localZ * MathF.Cos(_angleC) * MathF.Sin(_angleB) * MathF.Cos(_angleA)
                 + localZ * MathF.Sin(_angleC) * MathF.Sin(_angleA);
        }

        /// <summary>
        /// Rotates a 3D point around all three axes and returns the transformed Y coordinate.
        /// Uses the combined rotation matrix derived from the current angles A, B, and C.
        /// </summary>
        /// <param name="localX">Original X coordinate relative to cube center.</param>
        /// <param name="localY">Original Y coordinate relative to cube center.</param>
        /// <param name="localZ">Original Z coordinate relative to cube center.</param>
        /// <returns>The rotated Y coordinate.</returns>
        private float CalculateY(float localX, float localY, float localZ)
        {
            return localX * MathF.Sin(_angleC) * MathF.Cos(_angleB)
                 + localY * MathF.Sin(_angleC) * MathF.Sin(_angleB) * MathF.Sin(_angleA)
                 + localY * MathF.Cos(_angleC) * MathF.Cos(_angleA)
                 + localZ * MathF.Sin(_angleC) * MathF.Sin(_angleB) * MathF.Cos(_angleA)
                 - localZ * MathF.Cos(_angleC) * MathF.Sin(_angleA);
        }

        /// <summary>
        /// Rotates a 3D point around all three axes and returns the transformed Z coordinate.
        /// Used for depth calculation and z-buffering. Points behind the camera (Z &lt;= 0) are culled.
        /// </summary>
        /// <param name="localX">Original X coordinate relative to cube center.</param>
        /// <param name="localY">Original Y coordinate relative to cube center.</param>
        /// <param name="localZ">Original Z coordinate relative to cube center.</param>
        /// <returns>The rotated Z coordinate before camera translation.</returns>
        private float CalculateZ(float localX, float localY, float localZ)
        {
            return localX * -MathF.Sin(_angleB)
                 + localY * MathF.Cos(_angleB) * MathF.Sin(_angleA)
                 + localZ * MathF.Cos(_angleB) * MathF.Cos(_angleA);
        }

        /// <summary>
        /// Increments the rotation angles based on the configured speeds and direction.
        /// In Continuous mode, applies the <see cref="CubeSettings.ContinuousSpeedMultiplier"/>.
        /// Frozen axes are reset to zero each call. Direction can be forward, backward, or random.
        /// </summary>
        private void ApplyRotation()
        {
            int direction = 1;

            if (_settings.Cube.Direction == RotationDirection.Backward)
                direction = -1;
            else if (_settings.Cube.Direction == RotationDirection.Random)
                direction = _rnd.Next(_settings.Cube.RandomModeFrequency) * 2 - 1;

            float speedMultiplier = _settings.Cube.RotationMode == CubeRotationMode.Continuous ? _settings.Cube.ContinuousSpeedMultiplier: 1f;

            _angleA = _settings.Cube.FreezeXRotation ? 0f : _angleA + _settings.Cube.RotationSpeedX * direction * speedMultiplier;
            _angleB = _settings.Cube.FreezeYRotation ? 0f : _angleB + _settings.Cube.RotationSpeedY * direction * speedMultiplier;
            _angleC = _settings.Cube.FreezeZRotation ? 0f : _angleC + _settings.Cube.RotationSpeedZ * direction * speedMultiplier;
        }
    }
}

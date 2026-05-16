using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Config.Settings.Audio;
using TERMINAL_FREQUENCY.Visualization;


namespace TERMINAL_FREQUENCY.Core.Audio
{
    /// <summary>
    /// Captures audio from system output (loopback), physical input devices (microphone, line-in),
    /// or specific render device loopback. Performs RMS volume analysis, peak tracking,
    /// noise gating, volume smoothing, spike detection, and optional FFT frequency analysis.
    /// Fires events for continuous volume updates, audio spikes, and frequency band data.
    /// </summary>
    public class AudioCapture
    {
        /// <summary>Fires on every audio callback with the smoothed volume level.</summary>
        public event Action<float>? OnVolumeUpdated;

        /// <summary>Fires when a volume spike (beat) is detected. Passes the raw spike intensity.</summary>
        public event Action<float>? OnVolumeSpike;

        /// <summary>Fires on every audio callback with normalized frequency band data. Only when <see cref="IFrequencyReactive"/> visualization is active.</summary>
        public event Action<float[]>? OnFrequencyData;

        /// <summary>The current smoothed volume level (0 to RMS * multiplier).</summary>
        public float SmoothedVolume { get; private set; } = 0;

        /// <summary>The tracked peak volume with configurable decay.</summary>
        public float PeakVolume { get; private set; } = 0;

        /// <summary>The FFT frequency analyzer instance for spectrum visualization.</summary>
        public FftAnalyzer FftAnalyzer { get; private set; }

        /// <summary>The raw Root Mean Square value of the current audio buffer.</summary>
        public double RMS { get; set; } = 0;

        private WasapiLoopbackCapture? _capture;
        private IWaveIn? _waveIn;
        private int _deviceIndex = 0; //fallback audio device
        private Settings _settings;
        private IVisualization? _currentVisualization;
        private MMDeviceEnumerator _mmDeviceEnumerator;
        private MMDeviceCollection _devices;
        private string _deviceName;

        /// <summary>
        /// Creates an audio capture using the system default output device (loopback).
        /// </summary>
        /// <param name="settings">Application settings containing audio configuration.</param>
        public AudioCapture(Settings settings)
        {
            _settings = settings;
            FftAnalyzer ??= new FftAnalyzer(_settings);
        }

        /// <summary>
        /// Creates an audio capture targeting a specific device by index.
        /// The index corresponds to the list returned by <see cref="ConsoleWindow.Utility.GetAvailableDevices"/>.
        /// </summary>
        /// <param name="settings">Application settings containing audio configuration.</param>
        /// <param name="deviceIndex">The device index to capture from.</param>
        public AudioCapture(Settings settings, int deviceIndex)
        {
            _settings = settings;
            _deviceIndex = deviceIndex;
            FftAnalyzer ??= new FftAnalyzer(_settings);
        }

        /// <summary>
        /// Starts audio capture based on configuration. If <see cref="AudioCaptureSettings.SpecifyAudioDevice"/>
        /// is true, selects the specified device and automatically determines whether it is an input
        /// (microphone) or output (speakers/headphones) device. Otherwise captures system loopback.
        /// Throws <see cref="UnauthorizedAccessException"/> if microphone access is denied by Windows privacy settings.
        /// </summary>
        public void Start()
        {

            if(_settings.AudioCaptureSettings.SpecifyAudioDevice)
            {
                _mmDeviceEnumerator = new MMDeviceEnumerator();
                _devices = _mmDeviceEnumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active);
                var selectedDevice = _devices[_deviceIndex];
                _deviceName = selectedDevice.FriendlyName;

                if (selectedDevice.DataFlow == DataFlow.Capture)
                {
                    // Microphone / line-in
                    var capture = new WasapiCapture(selectedDevice);
                    capture.ShareMode = AudioClientShareMode.Shared;
                    capture.DataAvailable += OnDataAvailable;
                    try
                    {
                        capture.StartRecording();
                        _waveIn = capture;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Console.WriteLine("Microphone access denied. Please enable in Windows Settings → Privacy → Microphone.");
                    }

                }
                else
                {
                    // Render device loopback (headphones, speakers)
                    var loopback = new WasapiLoopbackCapture(selectedDevice);
                    loopback.DataAvailable += OnDataAvailable;
                    loopback.StartRecording();
                    _waveIn = loopback;
                }
            }
            else
            {
                var capture = new WasapiLoopbackCapture();
                capture.DataAvailable += OnDataAvailable;
                capture.StartRecording();
                _deviceName = "System Output (Loopback)";
                _waveIn = capture;
            }
            

        }

        /// <summary>Stops audio capture and disposes the capture device.</summary>
        public void Stop()
        {
            if (_waveIn != null)
            {
                _waveIn.StopRecording();
                _waveIn.Dispose();
                _waveIn = null;
            }
        }

        /// <summary>
        /// Processes incoming audio data. Calculates RMS volume, applies noise gate and smoothing,
        /// tracks peak volume with decay, and fires <see cref="OnVolumeUpdated"/>.
        /// Checks for volume spikes by comparing current volume against smoothed average
        /// and fires <see cref="OnVolumeSpike"/> when a spike is detected.
        /// If the current visualization implements <see cref="IFrequencyReactive"/>,
        /// runs FFT analysis via <see cref="FftAnalyzer"/> and fires <see cref="OnFrequencyData"/>.
        /// </summary>
        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            int sampleCount = e.BytesRecorded / _settings.AudioCaptureSettings.AudioSampleResolution;
            if (sampleCount == 0) return;

            double sumSquares = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                float sample = BitConverter.ToSingle(e.Buffer, i * _settings.AudioCaptureSettings.AudioSampleResolution);
                sumSquares += (double)sample * (double)sample;
            }

            RMS = Math.Sqrt(sumSquares / sampleCount);

            if (double.IsNaN(RMS) || double.IsInfinity(RMS))
                return;

            float volumeNow = (float)RMS * _settings.AudioCaptureSettings.RmsMultiplier;

            //noise gate
            if (volumeNow < _settings.AudioCaptureSettings.NoiseGateFloor)
                volumeNow = 0;

            //smooth out volume
            SmoothedVolume = SmoothedVolume * _settings.AudioCaptureSettings.SmoothingFactorExisting + volumeNow * _settings.AudioCaptureSettings.SmoothingFactorIncoming;

            //track peak
            if (volumeNow > PeakVolume && volumeNow > _settings.AudioCaptureSettings.PeakTrackingMinimum)
                PeakVolume = volumeNow;
            PeakVolume *= _settings.AudioCaptureSettings.PeakDecayFactor;

            //notif event listeners
            OnVolumeUpdated?.Invoke(SmoothedVolume);

            //Check for spikes
            if (volumeNow > _settings.AudioCaptureSettings.SpikeVolumeMinimum && volumeNow > SmoothedVolume * _settings.AudioCaptureSettings.SpikeRatio)
            {
                OnVolumeSpike?.Invoke(volumeNow);
                SmoothedVolume = volumeNow;
            }

            if (_currentVisualization is IFrequencyReactive)
            {
                if (_waveIn != null && FftAnalyzer != null)
                {
                    try
                    {
                        FftAnalyzer.Process(
                            e.Buffer,
                            e.BytesRecorded,
                            _settings.AudioCaptureSettings.AudioSampleResolution,
                            _waveIn.WaveFormat.Channels,
                            _waveIn.WaveFormat.SampleRate,
                            _settings.FftSettings.BandCount,
                            _settings.FftSettings.Sensitivity,
                            _settings.FftSettings.HighPass,
                            _settings.FftSettings.LowPass,
                            _settings.FftSettings.BassCutoff
                        );

                        if (FftAnalyzer.FrequencyBands != null)
                        {
                            OnFrequencyData?.Invoke(FftAnalyzer.FrequencyBands);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"FFT Error: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Updates the reference to the currently active visualization.
        /// Used to gate FFT processing: FFT only runs when an <see cref="IFrequencyReactive"/> visualization is active.
        /// </summary>
        /// <param name="visualization">The current visualization instance, or null.</param>
        public void UpdateCurrentVisualization(IVisualization visualization)
        {
            _currentVisualization = visualization;
        }

        /// <summary>
        /// Updates the settings reference. Call after loading new settings at runtime
        /// to ensure audio processing uses the latest configuration values.
        /// </summary>
        /// <param name="newSettings">The new settings object.</param>
        public void UpdateSettings(Settings newSettings)
        {
            _settings = newSettings;
        }

        /// <summary>
        /// Returns the friendly name of the currently active capture device.
        /// For loopback, returns "System Output (Loopback)". For WASAPI devices, returns the device's friendly name.
        /// </summary>
        /// <returns>The device name string, or "No device" if capture is not started.</returns>
        public string GetDeviceName()
        {
            if (_waveIn == null)
                return "No device";

            if (_waveIn is WaveInEvent waveIn)
                return WaveInEvent.GetCapabilities(waveIn.DeviceNumber).ProductName;

            return _deviceName; // Set during WASAPI capture setup
        }
    }
}
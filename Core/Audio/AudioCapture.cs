using NAudio.CoreAudioApi;
using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Text;
using TERMINAL_FREQUENCY.Config;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Visualization.Equalizer;
using TERMINAL_FREQUENCY.Visualization.Rings;

namespace TERMINAL_FREQUENCY.Core.Audio
{
    public class AudioCapture
    {
        public event Action<float>? OnVolumeUpdated;
        public event Action<float>? OnVolumeSpike;
        public event Action<float[]>? OnFrequencyData;

        private WasapiLoopbackCapture? _capture;
        private IWaveIn? _waveIn;
        private int _deviceIndex = 0; //fallback audio device
        private Settings _settings;
        private IVisualization? _currentVisualization;
        private MMDeviceEnumerator _mmDeviceEnumerator;
        private MMDeviceCollection _devices;
        private string _deviceName;
        public float SmoothedVolume { get; private set; } = 0;
        public float PeakVolume { get; private set; } = 0;
        public FftAnalyzer FftAnalyzer { get; private set; }
        public double RMS { get; set; } = 0;


        public AudioCapture(Settings settings)
        {
            _settings = settings;
            FftAnalyzer ??= new FftAnalyzer(_settings);
        }

        public AudioCapture(Settings settings, int deviceIndex)
        {
            _settings = settings;
            _deviceIndex = deviceIndex;
            FftAnalyzer ??= new FftAnalyzer(_settings);
        }

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

        public void Stop()
        {
            if (_waveIn != null)
            {
                _waveIn.StopRecording();
                _waveIn.Dispose();
                _waveIn = null;
            }
        }

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

        public void UpdateCurrentVisualization(IVisualization visualization)
        {
            _currentVisualization = visualization;
        }

        public void UpdateSettings(Settings newSettings)
        {
            _settings = newSettings;
        }

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
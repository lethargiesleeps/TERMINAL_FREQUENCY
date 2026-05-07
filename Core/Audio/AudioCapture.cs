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
        private int _deviceIndex = -1; //fallback audio device
        private Settings _settings;
        private IVisualization? _currentVisualization;

        public float SmoothedVolume { get; private set; } = 0;
        public float PeakVolume { get; private set; } = 0;
        public FftAnalyzer FftAnalyzer { get; private set; }
        public double RMS { get; set; } = 0;


        public AudioCapture(Settings settings)
        {
            _settings = settings;
            _deviceIndex = -1;
            FftAnalyzer ??= new FftAnalyzer(_settings);
        }

        public AudioCapture(Settings settings, int deviceIndex)
        {
            _settings = settings;
            _deviceIndex = deviceIndex < 0 ? -1 : deviceIndex;
            FftAnalyzer ??= new FftAnalyzer(_settings);
        }

        public void Start()
        {
            _capture = new WasapiLoopbackCapture();
            _capture.DataAvailable += OnDataAvailable;
            _capture.StartRecording();
        }

        public void Stop()
        {
            if (_capture != null)
            {
                _capture.StopRecording();
                _capture.Dispose();
                _capture = null;
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
                if (_capture != null && FftAnalyzer != null)
                {
                    try
                    {
                        FftAnalyzer.Process(
                            e.Buffer,
                            e.BytesRecorded,
                            _settings.AudioCaptureSettings.AudioSampleResolution,
                            _capture.WaveFormat.Channels,
                            _capture.WaveFormat.SampleRate,
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
    }
}
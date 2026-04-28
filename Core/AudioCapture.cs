using NAudio.Wave;
using System;
using TERMINAL_FREQUENCY.Config;

namespace TERMINAL_FREQUENCY.Core
{
    public class AudioCapture
    {
        public event Action<float>? OnVolumeUpdated;
        public event Action<float>? OnVolumeSpike;

        private WasapiLoopbackCapture? capture;
        private int _deviceIndex = -1; //fallback audio device

        public float SmoothedVolume { get; private set; } = 0;
        public float PeakVolume { get; private set; } = 0;
        public bool DebugMode { get; set; } = true;

        public AudioCapture()
        {
            _deviceIndex = -1;
        }

        public AudioCapture(int deviceIndex)
        {
            _deviceIndex = (deviceIndex < 0) ? -1 : deviceIndex;
        }
        public void Start()
        {
            capture = new WasapiLoopbackCapture();
            capture.DataAvailable += OnDataAvailable;
            capture.StartRecording();

            if(Config.Config.DEBUG_MODE)
                Console.WriteLine($"Audio capture started ({capture.WaveFormat.SampleRate}Hz)");
        }

        public void Stop()
        {
            if (capture != null)
            {
                capture.StopRecording();
                capture.Dispose();
                capture = null;
            }
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            int sampleCount = e.BytesRecorded / Config.Config.AUDIO_SAMPLE_RESOLUTION;
            if (sampleCount == 0) return;

            double sumSquares = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                float sample = BitConverter.ToSingle(e.Buffer, i * Config.Config.AUDIO_SAMPLE_RESOLUTION);
                sumSquares += (double)sample * (double)sample;
            }

            double rms = Math.Sqrt(sumSquares / sampleCount);

            if (double.IsNaN(rms) || double.IsInfinity(rms))
                return;

            float volumeNow = (float)rms * Config.Config.RMS_MULTIPLIER;

            //noise gate
            if (volumeNow < Config.Config.NOISE_GATE_THRESHHOLD)
                volumeNow = 0;

            //smooth out volume
            SmoothedVolume = SmoothedVolume * Config.Config.SMOOTHING_FACTOR_EXISTING + volumeNow * Config.Config.SMOOTHING_FACTOR_INCOMING;

            //track peak
            if (volumeNow > PeakVolume && volumeNow > Config.Config.PEAK_TRACKING_MINIMUM)
                PeakVolume = volumeNow;
            PeakVolume *= Config.Config.PEAK_DECAY_FACTOR;

            //notif event listeners
            OnVolumeUpdated?.Invoke(SmoothedVolume);

            //Check for spikes
            if (volumeNow > Config.Config.SPIKE_VOLUME_MINIMUM && volumeNow > SmoothedVolume * Config.Config.SPIKE_RATIO)
            {
                OnVolumeSpike?.Invoke(volumeNow);
                SmoothedVolume = volumeNow;
            }
        }
    }
}
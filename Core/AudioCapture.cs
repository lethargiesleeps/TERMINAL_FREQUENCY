using NAudio.Wave;
using System;
using TERMINAL_FREQUENCY.Config;

namespace TERMINAL_FREQUENCY.Core
{
    public class AudioCapture
    {
        public event Action<float> OnVolumeUpdated;
        public event Action<float> OnVolumeSpike;

        private WasapiLoopbackCapture capture;
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
            int sampleCount = e.BytesRecorded / Config.Config.BYTE_4;
            if (sampleCount == 0) return;

            double sumSquares = 0;

            for (int i = 0; i < sampleCount; i++)
            {
                float sample = BitConverter.ToSingle(e.Buffer, i * Config.Config.BYTE_4);
                sumSquares += (double)sample * (double)sample;
            }

            double rms = Math.Sqrt(sumSquares / sampleCount);

            if (double.IsNaN(rms) || double.IsInfinity(rms))
                return;

            float volumeNow = (float)rms * Config.Config.RMS_CEILING;

            //noise gate
            if (volumeNow < Config.Config.VOL_FLOOR)
                volumeNow = 0;

            //smooth out volume
            SmoothedVolume = SmoothedVolume * Config.Config.VOL_CORRECTOR_CEILING + volumeNow * Config.Config.VOL_CORRECTOR_FLOOR;

            //track peak
            if (volumeNow > PeakVolume && volumeNow > Config.Config.CLIPPING_THRESHOLD)
                PeakVolume = volumeNow;
            PeakVolume *= Config.Config.CLIPPING_PREVENTION;

            //notif event listeners
            OnVolumeUpdated?.Invoke(SmoothedVolume);

            //Check for spikes
            //TODO: FIND NAMES FOR THESE MAGIC NUMBERS
            if (volumeNow > 0.15f && volumeNow > SmoothedVolume * 1.4f)
            {
                OnVolumeSpike?.Invoke(volumeNow);
                SmoothedVolume = volumeNow;
            }
        }
    }
}
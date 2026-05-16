using NAudio.Dsp;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Config.Settings.Audio;

namespace TERMINAL_FREQUENCY.Core.Audio
{
    /// <summary>
    /// Performs Fast Fourier Transform (FFT) analysis on raw audio data to extract
    /// frequency band magnitudes. Converts time-domain audio samples into frequency-domain
    /// data using a Hamming window and logarithmic band mapping. Supports per-band peak
    /// normalization, configurable bass band isolation, and sensitivity adjustment.
    /// </summary>
    public class FftAnalyzer
    {
        private Complex[] _fftBuffer;
        private float[] _windowCoefficients;
        private int _fftLength;
        private float[] _bandPeaks;
        private float _highPassCutoff;
        private float _lowPassCutoff;
        private float _bassBandCutoff;
        private Settings _settings;
        private const float HAMMING_ALPHA = 0.54f;
        private const float HAMMING_BETA = 0.46f;
        private const float BASS_BAND_CUTOFF = 150f; //if more than 8 bands, 1st band always covers up to 150Hz

        /// <summary>Current frequency band magnitudes after processing. Values between 0 and 1.</summary>
        public float[] FrequencyBands { get; private set; } = [];

        /// <summary>
        /// Initializes the FFT analyzer with pre-computed Hamming window coefficients
        /// for the specified FFT length. The window reduces spectral leakage during frequency analysis.
        /// </summary>
        /// <param name="settings">Application settings for frequency configuration.</param>
        /// <param name="fftLength">Number of samples per FFT window. Must be a power of 2. Defaults to 1024.</param>
        public FftAnalyzer(Settings settings, int fftLength = 1024)
        {
            _settings = settings;
            _fftLength = fftLength;
            _fftBuffer = new Complex[fftLength];
            _windowCoefficients = new float[fftLength];
            _bandPeaks = [];

            for (int i = 0; i < _fftLength; i++)
                _windowCoefficients[i] = (float)(HAMMING_ALPHA - HAMMING_BETA * Math.Cos(2 * Math.PI * i / _fftLength));
        }

        /// <summary>
        /// Processes a raw audio buffer through the FFT pipeline. Extracts frequency magnitudes,
        /// maps them to logarithmic bands, and normalizes each band against its own tracked peak.
        /// When <see cref="FftSettings.DedicatedBassBand"/> is enabled, the first band is
        /// always mapped to the bass cutoff frequency range regardless of total band count.
        /// </summary>
        /// <param name="buffer">Raw audio byte buffer from the capture device.</param>
        /// <param name="bytesRecorded">Number of bytes recorded in this buffer.</param>
        /// <param name="bytesPerSample">Bytes per audio sample (typically 4 for 32-bit float).</param>
        /// <param name="channels">Number of audio channels in the capture format.</param>
        /// <param name="sampleRate">Sample rate of the audio in Hz (e.g., 44100).</param>
        /// <param name="numBands">Number of frequency bands to output.</param>
        /// <param name="bandSensitivity">Multiplier applied after normalization. Higher values push bands toward 1.0.</param>
        /// <param name="lowFreq">Lowest frequency to analyze in Hz. Frequencies below this are ignored.</param>
        /// <param name="highFreq">Highest frequency to analyze in Hz. Frequencies above this are ignored.</param>
        /// <param name="bassBandCutoff">If <see cref="FftSettings.DedicatedBassBand"/> is true, band 0 covers up to this Hz.</param>
        public void Process(byte[] buffer, int bytesRecorded, int bytesPerSample, int channels, float sampleRate, int numBands, float bandSensitivity = 1.0f, float lowFreq = 30f, float highFreq = 18000f, float bassBandCutoff = BASS_BAND_CUTOFF)
        {
            _highPassCutoff = lowFreq;
            _lowPassCutoff = highFreq;
            _bassBandCutoff = bassBandCutoff;

            int sampleCount = bytesRecorded / bytesPerSample;
            int monoSamples = sampleCount / channels;
            if (monoSamples < _fftLength) return;

            FrequencyBands = new float[numBands];
            if (_bandPeaks.Length != numBands)
                _bandPeaks = new float[numBands];

            int startIndex = monoSamples - _fftLength;

            for (int i = 0; i < _fftLength; i++)
            {
                float sum = 0;

                for (int channelIndex = 0; channelIndex < channels; channelIndex++)
                {
                    int index = ((startIndex + i) * channels + channelIndex) * bytesPerSample;
                    if (index < bytesRecorded) sum += BitConverter.ToSingle(buffer, index);
                }

                _fftBuffer[i].X = (sum / channels) * _windowCoefficients[i];
                _fftBuffer[i].Y = 0;
            }

            FastFourierTransform.FFT(true, (int)Math.Log(_fftLength, 2), _fftBuffer);

            for (int i = 0; i < _fftLength / 2; i++)
            {
                float freq = (i * sampleRate) / _fftLength;
                if (freq < lowFreq || freq > highFreq) continue;

                float magnitude = (float)Math.Sqrt(_fftBuffer[i].X * _fftBuffer[i].X + _fftBuffer[i].Y * _fftBuffer[i].Y);

                int band;

                if (_settings.Fft.DedicatedBassBand && freq <= bassBandCutoff)
                {
                    band = 0;
                }
                else if (_settings.Fft.DedicatedBassBand)
                {
                    float logFreq = (float)(Math.Log(freq / bassBandCutoff) / Math.Log(highFreq / bassBandCutoff));
                    band = 1 + (int)(logFreq * (numBands - 1));
                }
                else
                {
                    float logFreq = (float)(Math.Log(freq / lowFreq) / Math.Log(highFreq / lowFreq));
                    band = (int)(logFreq * numBands);
                }

                if (band >= 0 && band < numBands)
                    FrequencyBands[band] += magnitude;
            }

            //per-band peak normalization
            for (int i = 0; i < numBands; i++)
            {
                if (FrequencyBands[i] > _bandPeaks[i])
                    _bandPeaks[i] = FrequencyBands[i];
                _bandPeaks[i] *= 0.995f;
                if (_bandPeaks[i] < 0.001f) _bandPeaks[i] = 0.001f;

                FrequencyBands[i] = FrequencyBands[i] / _bandPeaks[i];
                FrequencyBands[i] = Math.Clamp(FrequencyBands[i], 0.01f, 1.0f);
                FrequencyBands[i] = Math.Min(1.0f, FrequencyBands[i] * bandSensitivity);
            }
        }

        /// <summary>
        /// Returns an array of human-readable strings describing each frequency band's range
        /// and current volume level. Respects the <see cref="FftSettings.DedicatedBassBand"/>
        /// setting for accurate range display. Useful for debug overlays.
        /// </summary>
        /// <param name="bandCount">Number of bands to describe.</param>
        /// <returns>Array of formatted strings like "BAND[0]: HZ:30-150 | VOL:0.856".</returns>
        public string[] GetBandFrequencyData(int bandCount)
        {
            if (FrequencyBands == null || FrequencyBands.Length == 0)
                return new string[] { "NO FREQUENCY DATA" };

            string[] data = new string[bandCount];

            for (int i = 0; i < bandCount; i++)
            {
                float bandLow, bandHigh;

                if (_settings.Fft.DedicatedBassBand && i == 0)
                {
                    bandLow = _highPassCutoff;
                    bandHigh = _bassBandCutoff;
                }
                else if (_settings.Fft.DedicatedBassBand)
                {
                    bandLow = (float)(_bassBandCutoff * Math.Pow(_lowPassCutoff / _bassBandCutoff, (float)(i - 1) / (bandCount - 1)));
                    bandHigh = (float)(_bassBandCutoff * Math.Pow(_lowPassCutoff / _bassBandCutoff, (float)i / (bandCount - 1)));
                }
                else
                {
                    bandLow = (float)(_highPassCutoff * Math.Pow(_lowPassCutoff / _highPassCutoff, (float)i / bandCount));
                    bandHigh = (float)(_highPassCutoff * Math.Pow(_lowPassCutoff / _highPassCutoff, (float)(i + 1) / bandCount));
                }

                float bandValue = i < FrequencyBands.Length ? FrequencyBands[i] : 0f;
                data[i] = $"BAND[{i}]: HZ:{bandLow:F0}-{bandHigh:F0} | VOL:{bandValue:F3}";
            }

            return data;
        }
    }
}
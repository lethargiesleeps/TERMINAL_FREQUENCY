using NAudio.Dsp;
using System;
using System.Diagnostics;
using System.Linq;
using TERMINAL_FREQUENCY.Config.Settings;

namespace TERMINAL_FREQUENCY.Core.Audio
{
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
        public float[] FrequencyBands { get; private set; } = [];

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

                if (_settings.FftSettings.DedicatedBassBand && freq <= bassBandCutoff)
                {
                    band = 0;
                }
                else if (_settings.FftSettings.DedicatedBassBand)
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

        public string[] GetBandFrequencyData(int bandCount)
        {
            if (FrequencyBands == null || FrequencyBands.Length == 0)
                return new string[] { "No data" };

            string[] data = new string[bandCount];

            for (int i = 0; i < bandCount; i++)
            {
                float bandLow, bandHigh;

                if (_settings.FftSettings.DedicatedBassBand && i == 0)
                {
                    bandLow = _highPassCutoff;
                    bandHigh = _bassBandCutoff;
                }
                else if (_settings.FftSettings.DedicatedBassBand)
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
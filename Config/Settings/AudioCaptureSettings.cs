using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class AudioCaptureSettings : IConfigurable
    {
        public bool SpecifyAudioDevice { get; set; }                   //TODO: lets user select which audio device to capture, not implemented
        public int AudioSampleResolution { get; set; }                 //bytes per sample (typically 4, can be 2 or 4)
        public float RmsMultiplier { get; set; }                       //scale RMS to the useable volume, safe range 10-1000
        public float NoiseGateFloor { get; set; }                      //ignores audio below set volume, higher kills quiet sounds, lower keeps noise. can be used to cut out device 'static/humming' that would trigger a visualization
        public float SmoothingFactorExisting { get; set; }             //existing + incoming is always = to 1, controls how quickly volume reacts to a change (how quickly vol is updated)
        public float SmoothingFactorIncoming { get; set; }             //see above
        public float PeakTrackingMinimum { get; set; }                 //range to track peaks, prevents noise from becoming a peak (0.05 to 0.3ish for best results)
        public float PeakDecayFactor { get; set; }                     //higher value = hold peak longer for dramatic effect, lower is more responsive (tested safe range of 0.95 - 0.999)
        public float SpikeVolumeMinimum { get; set; }                  //minimum volume to even consider a reaction (tested safe range 0.01 - 0.2)
        public float SpikeRatio { get; set; }                          //how much louder than calculated volume to trigger spike, lower = more sensitive 

        public AudioCaptureSettings()
        {
            Restore();
        }

        public void EnforceConstraints()
        {
            if(RmsMultiplier < 10) RmsMultiplier = 10f;
            if(RmsMultiplier > 1000) RmsMultiplier = 1000f;

            if (PeakTrackingMinimum < 0.05) PeakTrackingMinimum = 0.05f;
            if (PeakTrackingMinimum > 0.4) PeakTrackingMinimum = 0.4f;

            if (PeakDecayFactor < 0.9) PeakDecayFactor = 0.9f;
            if (PeakDecayFactor > 0.995) PeakDecayFactor = 0.995f;

            if (SpikeVolumeMinimum > 0.2) SpikeVolumeMinimum = 0.2f;

        }

        public void EnforceMandatoryConstraints()
        {
            if (AudioSampleResolution != 2 && AudioSampleResolution != 4) AudioSampleResolution = 4;
            if (RmsMultiplier < 0.1) RmsMultiplier = 0.1f;
            if (NoiseGateFloor < 0) NoiseGateFloor = 0.01f;
            if (SmoothingFactorExisting + SmoothingFactorIncoming != 1)
            {
                SmoothingFactorExisting = 0.8f;
                SmoothingFactorIncoming = 0.2f;
            }
            if (PeakTrackingMinimum < 0.01) PeakTrackingMinimum = 0.01f;

            if (PeakDecayFactor < 0.01) PeakDecayFactor = 0.01f;
            if (PeakDecayFactor > 1) PeakDecayFactor = 0.9999f;

            if (SpikeVolumeMinimum < 0.01) SpikeVolumeMinimum = 0.01f;

            if (SpikeRatio < 0.01) SpikeRatio = 0.01f;
        }

        public void Restore()
        {
            SpecifyAudioDevice = false;
            AudioSampleResolution = 4;
            RmsMultiplier = 100f;
            NoiseGateFloor = 0.1f;
            SmoothingFactorExisting = 0.8f;
            SmoothingFactorIncoming = 0.2f;
            PeakTrackingMinimum = 0.1f;
            PeakDecayFactor = 0.05f;
            SpikeVolumeMinimum = 0.05f;
            SpikeRatio = 1.15f;
        }
    }
}

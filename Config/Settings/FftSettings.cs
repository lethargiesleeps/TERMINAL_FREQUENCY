namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class FftSettings : IConfigurable
    {
        public int BandCount { get; set; }                 //how many seperate bands appear on screen
        public float Sensitivity { get; set; }             //adjust sensitivity of Frequency based visuals
        public bool DedicatedBassBand {  get; set; }       //if true, first band in a Frequency reactive visual is always between LowCutoff and BassCutoff, then the rest of the bands are programatically adjusted.
        public float HighPass { get; set; }                  //ignores all frequency data below set value
        public float LowPass { get; set; }                 //ignores all frequency data above this value
        public float BassCutoff { get; set; }
        public void EnforceConstraints()
        {
            if (Sensitivity < 0.1f) Sensitivity = 0.1f;
            if (Sensitivity > 5.0f) Sensitivity = 5.0f;
            if (HighPass < 10f) HighPass = 10f;
            if (LowPass < 15000f) LowPass = 15000f;
            if (BassCutoff > 300f) BassCutoff = 300f;
        }

        public void EnforceMandatoryConstraints()
        {
            if (BandCount % 2 != 0) BandCount++;
            if (BandCount < 4) BandCount = 4; 
            if (Sensitivity < 0.01f) Sensitivity = 0.1f;
            if (HighPass < 1f) HighPass = 1f;
            if (HighPass > 50f) HighPass = 50f;
            if (HighPass >= LowPass) HighPass = 50f;
            if (DedicatedBassBand && HighPass >= BassCutoff) HighPass = BassCutoff - 1f;
            if (LowPass > 20000f) LowPass = 20000f;
            if (LowPass <= HighPass || (DedicatedBassBand && HighPass <= BassCutoff)) LowPass = 18000f;
            if (BassCutoff <= HighPass || BassCutoff >= LowPass) BassCutoff = 150f;
        }

        public void Restore()
        {
            BandCount = 8;
            Sensitivity = 1.0f;
            DedicatedBassBand = true;
            HighPass = 30f;
            LowPass = 18000f;
            BassCutoff = 150f;
        }
    }
}

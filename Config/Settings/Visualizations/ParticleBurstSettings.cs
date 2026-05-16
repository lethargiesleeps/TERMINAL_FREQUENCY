namespace TERMINAL_FREQUENCY.Config.Settings.Visualizations
{
    public class ParticleBurstSettings : IConfigurable
    {
        public ConsoleColor Color { get; set; }
        public string CharacterSet { get; set; }
        public int ParticleCount { get; set; }
        public float SpeedMin { get; set; }
        public float SpeedMax { get; set; }
        public float LifeMin { get; set; }
        public float LifeMax { get; set; }
        public float FadeRate { get; set; }
        public float SpreadAngle { get; set; }
        public int MaxBursts { get; set; }
        public int BurstsPerSpike { get; set; }

        public ParticleBurstSettings()
        {
            Restore();
        }

        public void Restore()
        {
            Color = ConsoleColor.White;
            CharacterSet = "*+.oO0@";
            ParticleCount = 50;
            SpeedMin = 0.5f;
            SpeedMax = 1.0f;
            LifeMin = 0.3f;
            LifeMax = 1.0f;
            FadeRate = 0.02f;
            SpreadAngle = 360f;
            MaxBursts = 8;
            BurstsPerSpike = 3;
        }

        public void EnforceConstraints()
        {
            if (ParticleCount < 1) ParticleCount = 1;
            if (ParticleCount > 200) ParticleCount = 200;
            if (SpeedMin < 0.1f) SpeedMin = 0.1f;
            if (SpeedMax < SpeedMin) SpeedMax = SpeedMin;
            if (LifeMin < 0.1f) LifeMin = 0.1f;
            if (LifeMax < LifeMin) LifeMax = LifeMin;
            if (FadeRate < 0.001f) FadeRate = 0.001f;
            if (FadeRate > 0.1f) FadeRate = 0.1f;
            if (SpreadAngle < 10f) SpreadAngle = 10f;
            if (SpreadAngle > 360f) SpreadAngle = 360f;
            if (MaxBursts < 1) MaxBursts = 1;
            if (MaxBursts > 20) MaxBursts = 20;
        }

        public void EnforceMandatoryConstraints()
        {
            if (ParticleCount < 1) ParticleCount = 1;
            if (SpeedMin < 0f) SpeedMin = 0f;
            if (SpeedMax < 0f) SpeedMax = 0f;
            if (LifeMin < 0f) LifeMin = 0f;
            if (LifeMax < 0f) LifeMax = 0f;
            if (FadeRate < 0f) FadeRate = 0.01f;
            if (MaxBursts < 1) MaxBursts = 1;
            if (string.IsNullOrEmpty(CharacterSet)) CharacterSet = "*+.";
            if(BurstsPerSpike < 0) BurstsPerSpike = 0;
        }
    }
}

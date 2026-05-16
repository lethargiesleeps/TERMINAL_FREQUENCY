namespace TERMINAL_FREQUENCY.Config.Settings.Visualizations
{
    public class NoiseFieldSettings : IConfigurable
    {
        public ConsoleColor Color { get; set; }
        public bool UseColorPattern { get; set; } 
        public ConsoleColor[] ColorPattern { get; set; }
        public string CharacterSet { get; set; }
        public bool UseDualCharacterSets { get; set; }
        public string QuietCharacterSet { get; set; }
        public string LoudCharacterSet { get; set; }
        public float CharacterSwitchThreshold { get; set; } 
        public float CharacterChangeRate { get; set; } //0 = never change, 1 = every frame
        public float MinDensity { get; set; }
        public float MaxDensity { get; set; }
        public float SpreadRadius { get; set; }
        public float JitterAmount { get; set; }
        public bool CenterOrigin { get; set; }
        public float VolumeThreshold { get; set; }
        public float Sensitivity { get; set; }
        public float DecayRate { get; set; }

        public NoiseFieldSettings()
        {
            Restore();
        }

        public void Restore()
        {
            Color = ConsoleColor.White;
            CharacterSet = "!@#$%^&*()_+-=[]{}|;:',.<>?/`~";
            QuietCharacterSet = ".,-~:;'`";
            LoudCharacterSet = "#@%&*!$";
            MinDensity = 0.02f;
            MaxDensity = 0.4f;
            SpreadRadius = 0.45f;
            JitterAmount = 0.3f;
            CenterOrigin = true;
            VolumeThreshold = 0.5f;
            DecayRate = 0.9f;
            CharacterSwitchThreshold = 0.5f;
            CharacterChangeRate = 1f;
            Sensitivity = 1.0f;
            UseDualCharacterSets = true;
            UseColorPattern = false;
            ColorPattern = new ConsoleColor[] { ConsoleColor.White, ConsoleColor.Cyan, ConsoleColor.Magenta };
            ;
        }

        public void EnforceConstraints()
        {
            if (MinDensity > 1f) MinDensity = 1f;
            if (MaxDensity < MinDensity) MaxDensity = MinDensity;
            if (MaxDensity > 1f) MaxDensity = 1f;
            if (SpreadRadius > 1f) SpreadRadius = 1f;
            if (JitterAmount > 1f) JitterAmount = 1f;
            if (VolumeThreshold > 1f) VolumeThreshold = 1f;
        }

        public void EnforceMandatoryConstraints()
        {
            if (MinDensity < 0f) MinDensity = 0f;
            if (MaxDensity < 0f) MaxDensity = 0f;
            if (SpreadRadius < 0f) SpreadRadius = 0.01f;
            if (JitterAmount < 0f) JitterAmount = 0.01f;
            if (SpreadRadius > 1f) SpreadRadius = 1f;
            if (JitterAmount > 1f) JitterAmount = 1f;
            if (VolumeThreshold < 0f) VolumeThreshold = 0.01f;
            if (VolumeThreshold > 1f) VolumeThreshold = 1f;
            if (string.IsNullOrEmpty(CharacterSet))
                CharacterSet = "!@#$%^&*()";
            if (DecayRate < 0.01f) DecayRate = 0.01f;
            if (Sensitivity < 0.1f) Sensitivity = 0.1f;
            if (Sensitivity > 10f) Sensitivity = 10f;
            if (CharacterChangeRate < 0.01f) CharacterChangeRate = 0.01f;
            if (CharacterChangeRate > 1f) CharacterChangeRate = 1f;
        }
    }
}

namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class Settings : IConfigurable
    {
        public GlobalSettings GlobalSettings { get; private set; }
        public FontSettings FontSettings { get; private set; }
        public RendererSettings RendererSettings { get; private set; }
        public ConsoleSettings ConsoleSettings { get; private set; }
        public AudioCaptureSettings AudioCaptureSettings { get; private set; }
        public FftSettings FftSettings { get; private set; }
        public RingsSettings RingsSettings { get; private set; }
        public WaterfallSettings WaterfallSettings { get; private set; }
        public ShapeSettings ShapeSettings { get; private set; }
        public EqualizerSettings EqualizerSettings { get; private set; }

        public Settings()
        {
            GlobalSettings = new GlobalSettings();
            FontSettings = new FontSettings();
            RendererSettings = new RendererSettings();
            ConsoleSettings = new ConsoleSettings();
            AudioCaptureSettings = new AudioCaptureSettings();
            FftSettings = new FftSettings();
            RingsSettings = new RingsSettings();
            WaterfallSettings = new WaterfallSettings();
            ShapeSettings = new ShapeSettings();
            EqualizerSettings = new EqualizerSettings();
        }

        public void Restore()
        {
            GlobalSettings.Restore();
            FontSettings.Restore();
            RendererSettings.Restore();
            ConsoleSettings.Restore();
            AudioCaptureSettings.Restore();
            FftSettings.Restore();
            RingsSettings.Restore();
            WaterfallSettings.Restore();
            ShapeSettings.Restore();
            EqualizerSettings.Restore();
        }

        public void EnforceConstraints()
        {
            GlobalSettings.EnforceConstraints();
            FontSettings.EnforceConstraints();
            RendererSettings.EnforceConstraints();
            ConsoleSettings.EnforceConstraints();
            AudioCaptureSettings.EnforceConstraints();
            FftSettings.EnforceConstraints();
            RingsSettings.EnforceConstraints();
            WaterfallSettings.EnforceConstraints();
            ShapeSettings.EnforceConstraints();
            EqualizerSettings.EnforceConstraints();
        }

        public void EnforceMandatoryConstraints()
        {
            GlobalSettings.EnforceMandatoryConstraints();
            FontSettings.EnforceMandatoryConstraints();
            RendererSettings.EnforceMandatoryConstraints();
            ConsoleSettings.EnforceMandatoryConstraints();
            AudioCaptureSettings.EnforceMandatoryConstraints();
            FftSettings.EnforceMandatoryConstraints();
            RingsSettings.EnforceMandatoryConstraints();
            WaterfallSettings.EnforceMandatoryConstraints();
            ShapeSettings.EnforceMandatoryConstraints();
            EqualizerSettings.EnforceMandatoryConstraints();
        }
    }
}

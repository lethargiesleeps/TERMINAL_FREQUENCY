namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class Settings : IConfigurable
    {
        public GlobalSettings GlobalSettings { get; private set; }
        public FontSettings FontSettings { get; private set; }
        public RendererSettings RendererSettings { get; private set; }
        public ConsoleSettings ConsoleSettings { get; private set; }
        public AudioCaptureSettings AudioCaptureSettings { get; private set; }
        public RingsSettings RingsSettings { get; private set; }
        public WaterfallSettings WaterfallSettings { get; private set; }
        public ShapeSettings ShapeSettings { get; private set; }
        public Settings()
        {
            GlobalSettings = new GlobalSettings();
            FontSettings = new FontSettings();
            RendererSettings = new RendererSettings();
            ConsoleSettings = new ConsoleSettings();
            AudioCaptureSettings = new AudioCaptureSettings();
            RingsSettings = new RingsSettings();
            WaterfallSettings = new WaterfallSettings();
            ShapeSettings = new ShapeSettings();
        }

        public void Restore()
        {
            GlobalSettings.Restore();
            FontSettings.Restore();
            RendererSettings.Restore();
            ConsoleSettings.Restore();
            AudioCaptureSettings.Restore();
            RingsSettings.Restore();
            WaterfallSettings.Restore();
            ShapeSettings.Restore();
        }

        public void EnforceConstraints()
        {
            GlobalSettings.EnforceConstraints();
            FontSettings.EnforceConstraints();
            RendererSettings.EnforceConstraints();
            ConsoleSettings.EnforceConstraints();
            AudioCaptureSettings.EnforceConstraints();
            RingsSettings.EnforceConstraints();
            WaterfallSettings.EnforceConstraints();
            ShapeSettings.EnforceConstraints();
        }

        public void EnforceMandatoryConstraints()
        {
            GlobalSettings.EnforceMandatoryConstraints();
            FontSettings.EnforceMandatoryConstraints();
            RendererSettings.EnforceMandatoryConstraints();
            ConsoleSettings.EnforceMandatoryConstraints();
            AudioCaptureSettings.EnforceMandatoryConstraints();
            RingsSettings.EnforceMandatoryConstraints();
            WaterfallSettings.EnforceMandatoryConstraints();
            ShapeSettings.EnforceMandatoryConstraints();
        }
    }
}

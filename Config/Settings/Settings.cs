using Newtonsoft.Json;

namespace TERMINAL_FREQUENCY.Config.Settings
{
    /// <summary>
    /// Main settings class that holds an instance of each individual Settings category.
    /// This is the class used for JSON serialization
    /// </summary>
    public class Settings : IConfigurable
    {
        [JsonProperty("Global")]
        public GlobalSettings GlobalSettings { get; private set; }

        [JsonProperty("Font")]
        public FontSettings FontSettings { get; private set; }

        [JsonProperty("Rendering")]
        public RendererSettings RendererSettings { get; private set; }

        [JsonProperty("Window")]
        public ConsoleSettings ConsoleSettings { get; private set; }

        [JsonProperty("AudioCapture")]
        public AudioCaptureSettings AudioCaptureSettings { get; private set; }

        [JsonProperty("FFT")]
        public FftSettings FftSettings { get; private set; }

        [JsonProperty("Rings")]
        public RingsSettings RingsSettings { get; private set; }

        [JsonProperty("Waterfall")]
        public WaterfallSettings WaterfallSettings { get; private set; }

        [JsonProperty("Shape")]
        public ShapeSettings ShapeSettings { get; private set; }

        [JsonProperty("Equalizer")]
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

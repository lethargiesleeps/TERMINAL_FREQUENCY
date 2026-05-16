using Newtonsoft.Json;
using TERMINAL_FREQUENCY.Config.Settings.Audio;
using TERMINAL_FREQUENCY.Config.Settings.General;
using TERMINAL_FREQUENCY.Config.Settings.Visualizations;

namespace TERMINAL_FREQUENCY.Config.Settings
{
    /// <summary>
    /// Main settings class that holds an instance of each individual Settings category.
    /// This is the class used for JSON serialization
    /// </summary>
    public class Settings : IConfigurable
    {
        [JsonProperty("Global")]
        public GlobalSettings Global { get; private set; }

        [JsonProperty("Font")]
        public FontSettings Font { get; private set; }

        [JsonProperty("Rendering")]
        public RendererSettings Renderer { get; private set; }

        [JsonProperty("Window")]
        public WindowSettings Window { get; private set; }

        [JsonProperty("AudioCapture")]
        public AudioCaptureSettings AudioCapture { get; private set; }

        [JsonProperty("FFT")]
        public FftSettings Fft { get; private set; }

        [JsonProperty("Rings")]
        public RingsSettings Rings { get; private set; }

        [JsonProperty("Waterfall")]
        public WaterfallSettings Waterfall { get; private set; }

        [JsonProperty("Shape")]
        public ShapeSettings Shape { get; private set; }

        [JsonProperty("Equalizer")]
        public EqualizerSettings Equalizer { get; private set; }

        [JsonProperty("Cube")]
        public CubeSettings Cube { get; private set; }

        [JsonProperty("NoiseField")]
        public NoiseFieldSettings NoiseField { get; private set; }

        [JsonProperty("ParticleBurst")]
        public ParticleBurstSettings ParticleBurst { get; private set; }

        public Settings()
        {
            Global = new GlobalSettings();
            Font = new FontSettings();
            Renderer = new RendererSettings();
            Window = new WindowSettings();
            AudioCapture = new AudioCaptureSettings();
            Fft = new FftSettings();
            Rings = new RingsSettings();
            Waterfall = new WaterfallSettings();
            Shape = new ShapeSettings();
            Equalizer = new EqualizerSettings();
            Cube = new CubeSettings();
            NoiseField = new NoiseFieldSettings();
            ParticleBurst = new ParticleBurstSettings();
        }

        public void Restore()
        {
            Global.Restore();
            Font.Restore();
            Renderer.Restore();
            Window.Restore();
            AudioCapture.Restore();
            Fft.Restore();
            Rings.Restore();
            Waterfall.Restore();
            Shape.Restore();
            Equalizer.Restore();
            Cube.Restore();
            NoiseField.Restore();
            ParticleBurst.Restore();
        }

        public void EnforceConstraints()
        {
            Global.EnforceConstraints();
            Font.EnforceConstraints();
            Renderer.EnforceConstraints();
            Window.EnforceConstraints();
            AudioCapture.EnforceConstraints();
            Fft.EnforceConstraints();
            Rings.EnforceConstraints();
            Waterfall.EnforceConstraints();
            Shape.EnforceConstraints();
            Equalizer.EnforceConstraints();
            Cube.EnforceConstraints();
            NoiseField.EnforceConstraints();
            ParticleBurst.EnforceConstraints();
        }

        public void EnforceMandatoryConstraints()
        {
            Global.EnforceMandatoryConstraints();
            Font.EnforceMandatoryConstraints();
            Renderer.EnforceMandatoryConstraints();
            Window.EnforceMandatoryConstraints();
            AudioCapture.EnforceMandatoryConstraints();
            Fft.EnforceMandatoryConstraints();
            Rings.EnforceMandatoryConstraints();
            Waterfall.EnforceMandatoryConstraints();
            Shape.EnforceMandatoryConstraints();
            Equalizer.EnforceMandatoryConstraints();
            Cube.EnforceMandatoryConstraints();
            NoiseField.EnforceMandatoryConstraints();
            ParticleBurst.EnforceMandatoryConstraints();
        }
    }
}

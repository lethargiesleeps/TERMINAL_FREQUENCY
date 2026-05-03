namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class Settings : IConfigurable
    {
        public GlobalSettings GlobalSettings { get; private set; }
        public FontSettings FontSettings { get; private set; }
        public RendererSettings RendererSettings { get; private set; }
        public ConsoleSettings ConsoleSettings { get; private set; }

        public Settings()
        {
            GlobalSettings = new GlobalSettings();
            FontSettings = new FontSettings();
            RendererSettings = new RendererSettings();
            ConsoleSettings = new ConsoleSettings();
        }

        public void Restore()
        {
            GlobalSettings = new GlobalSettings();
            FontSettings = new FontSettings();
            RendererSettings = new RendererSettings();
            ConsoleSettings = new ConsoleSettings();
        }

        public void EnforceConstraints()
        {
            GlobalSettings.EnforceConstraints();
            FontSettings.EnforceConstraints();
            RendererSettings.EnforceConstraints();
            ConsoleSettings.EnforceConstraints();
        }

        public void EnforceMandatoryConstraints()
        {
            GlobalSettings.EnforceMandatoryConstraints();
            FontSettings.EnforceMandatoryConstraints();
            RendererSettings.EnforceMandatoryConstraints();
            ConsoleSettings.EnforceMandatoryConstraints();
        }
    }
}

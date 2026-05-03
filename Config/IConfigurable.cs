namespace TERMINAL_FREQUENCY.Config
{
    public interface IConfigurable
    {
        void Restore();
        void EnforceConstraints();
        void EnforceMandatoryConstraints();
    }
}

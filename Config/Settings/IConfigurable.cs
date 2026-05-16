namespace TERMINAL_FREQUENCY.Config.Settings
{
    /// <summary>
    /// Defines a configurable settings object that supports restoring defaults
    /// and enforcing value constraints. All settings classes implement this interface
    /// to ensure consistent initialization, validation, and reset behavior.
    /// </summary>
    public interface IConfigurable
    {
        /// <summary>
        /// Resets all properties to their default values.
        /// Called at object construction and when the user triggers a settings reset.
        /// </summary>
        void Restore();

        /// <summary>
        /// Enforces recommended safe ranges for all properties.
        /// Values outside documented safe ranges are clamped to the nearest valid value.
        /// Called when <see cref="GlobalSettings.EnableSafeMode"/> is true during settings load.
        /// </summary>
        void EnforceConstraints();

        /// <summary>
        /// Enforces mandatory constraints that prevent program crashes.
        /// Checks for null arrays, out-of-range enum values, and values that would cause
        /// exceptions. Always called during settings load, regardless of safe mode.
        /// </summary>
        void EnforceMandatoryConstraints();
    }
}

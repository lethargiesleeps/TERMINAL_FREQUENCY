namespace TERMINAL_FREQUENCY.Visualization
{
    /// <summary>
    /// Interface for visualizations that react to volume data from audio capture.
    /// Any implementation must also implement IVisualization
    /// </summary>
    /// <see cref="IVisualization"/>
    public interface IVolumeReactive : IVisualization
    {
        /// <summary>
        /// Runs an update to calculations based on new volume amount.
        /// </summary>
        /// <param name="volume">The volume amount to process and convert to visuals.</param>
        void Update(float volume);
    }
}

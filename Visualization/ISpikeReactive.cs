namespace TERMINAL_FREQUENCY.Visualization
{
    /// <summary>
    /// Interface for visualizations that react to spike data.
    /// Any implementation must also implement IVisualization
    /// </summary>
    /// <see cref="IVisualization"/>
    public interface ISpikeReactive : IVisualization
    {
        /// <summary>
        /// When a spike is registered, code in these functions runs.
        /// </summary>
        void OnSpike();

        /// <summary>
        /// When a spike is registered, code in these functions run, with an extra intensity parameter (difference between new spike and previous spike usually)
        /// </summary>
        /// <param name="intensity">Intensity of new registered spike.</param>
        void OnSpike(float intensity);
    }
}

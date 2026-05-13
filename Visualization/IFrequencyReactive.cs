/// <summary>
/// Interface for visualizations that react to Fast Fourier Transform frequency data.
/// Any implementation must also implement IVisualization
/// </summary>
/// <see cref="IVisualization"/>
namespace TERMINAL_FREQUENCY.Visualization
{
    public interface IFrequencyReactive : IVisualization
    {
        /// <summary>
        /// When frequency data updates, code in these functions will run with new data passed via 'bands'
        /// </summary>
        /// <param name="bands">Data for each bandwidth (x Hertz to y Hertz) across the whole allowed spectrum.</param>
        void OnFrequencyData(float[] bands);
    }
}

namespace TERMINAL_FREQUENCY.Visualization
{
    public interface ISpikeReactive : IVisualization
    {
        void OnSpike();
        void OnSpike(float intensity);
    }
}

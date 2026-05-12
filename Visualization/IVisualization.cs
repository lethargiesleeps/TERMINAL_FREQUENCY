using TERMINAL_FREQUENCY.Core.Rendering;

namespace TERMINAL_FREQUENCY.Visualization
{
    /// <summary>
    /// Basic interface that every Visualization must implement.
    /// </summary>
    public interface IVisualization
    {
        public string Name { get; } //internal name of visualization
        public int ModeIndex { get; } //corresponding index, cannot be a duplicate and must follow order set out it VisualizationMode

        /// <summary>
        /// Main function that sends buffer data to the Renderer to be 'drawn'.
        /// </summary>
        /// <param name="buffer">The singleton ScreenBuffer to process buffer data, a.k.a the renderer.</param>
        void Draw(ScreenBuffer buffer);
    }
}

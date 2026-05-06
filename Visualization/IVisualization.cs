using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Core.Rendering;

namespace TERMINAL_FREQUENCY.Visualization
{
    public interface IVisualization
    {
        public string Name { get; }
        public int ModeIndex { get; }
        void Update(float volume);
        void Draw(ScreenBuffer buffer);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TERMINAL_FREQUENCY.Visualization
{
    public interface IFrequencyReactive : IVisualization
    {
        void OnFrequencyData(float[] bands);
    }
}

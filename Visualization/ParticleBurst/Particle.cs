using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TERMINAL_FREQUENCY.Visualization.ParticleBurst
{
    public class Particle
    {
        public float X {  get; set; }
        public float Y { get; set; }
        public float Vx { get; set; }
        public float Vy { get; set; }
        public float Life { get; set; }
        public float MaxLife { get; set; }
        public char Character { get; set; }
    }
}

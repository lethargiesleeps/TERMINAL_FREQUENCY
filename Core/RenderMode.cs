using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TERMINAL_FREQUENCY.Core
{
    //in order of slow to fast
    public enum RenderMode
    {
        PerPixel, //renders pixel by pixel, checks every available cell
        DirtyRect, //only renders regions of window that have changed since last frame
        RowBatched, //renders row by row, formulates the row then renders... really fast but can only render one foreground colour
        DirectWrite //renders entire buffer at once, effectively bypassing cursor writing, color overhead, pixel positions and other overhead
    }
}

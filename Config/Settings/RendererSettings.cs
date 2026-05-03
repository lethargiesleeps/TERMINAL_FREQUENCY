using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Core;

namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class RendererSettings : IConfigurable
    {
        public int TargetFps { get; set; }                //not regarded if THREAD_SLEEP or SPIN_WAIT is disabled                
        public bool EnableYield { get; set; }             //if true, yield for THREAD_RATE ms on every frame, prioritized over SpinWait.
        public int YieldTimeout { get; set; }             //in miliseconds, he higher the slower... approx FPS values are [1 = ~ 1000fps (max speed, max cpu usage, beware!), 8 = ~120fps, 16 = ~60fps, 33 = ~30fps, 50 = ~20fps, 100 = ~10fps] (safe range 8-100)
        public bool EnableSpinWait { get; set; }          //do a wait instead of shutting off the thread
        public int SpinWaitIterations { get; set; }       //how many times the thread spins before resuming, lower = faster
        public bool EnableThreadPriority { get; set; }    //if true, set thread priority at program launch. can be lowered or highered depending on what audio software is running
        public ThreadPriority ThreadPriority { get; set; }
        public RenderMode RendererMode { get; set; }      //switch rendering mode between PerPixel, DirtyRect, RowBatched and DirectWrite
        public ConsoleColor RowBatchColor { get; set; }   //since RowBatched rendering is monochrome, use this to set its foreground colour

        public RendererSettings()
        {
            Restore();
        }

        public void Restore()
        {
            TargetFps = 120;
            EnableYield = false;
            YieldTimeout = 33;
            EnableSpinWait = false;
            SpinWaitIterations = 10;
            EnableThreadPriority = false;
            ThreadPriority = ThreadPriority.Normal;
            RendererMode = RenderMode.DirectWrite;
            RowBatchColor = ConsoleColor.White;
        }

        public void EnforceConstraints()
        {
            if (YieldTimeout > 5000) YieldTimeout = 5000;
            if (SpinWaitIterations > 3000) SpinWaitIterations = 3000;
        }

        public void EnforceMandatoryConstraints()
        {
            if (YieldTimeout < 1) YieldTimeout = 1;
            if (SpinWaitIterations < 1) SpinWaitIterations = 1;

            if ((int)RendererMode < 0 || (int)RendererMode > Utility.EnumCount<RenderMode>(true))
                RendererMode = RenderMode.DirectWrite;

            if ((int)RowBatchColor < 0 || (int)RowBatchColor > Utility.EnumCount<ConsoleColor>(true))
                RowBatchColor = ConsoleColor.White;

            if ((int)ThreadPriority < 0 || (int)ThreadPriority > Utility.EnumCount<ThreadPriority>(true))
                ThreadPriority = ThreadPriority.Normal;
        }

        public Dictionary<string, object> GetProperties<T>(T obj)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Config.Font;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Visualization;

namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class GlobalSettings : IConfigurable
    {
        public bool BypassStartupScreen { get; set; }                  //if true, skip startup and launch directly into visuals
        public bool ForceDefaultSettings { get; set; }                 //TODO: if true, and a settings.json file is read, it ignores any updates and uses default settings
        public bool EnableDebugMode { get; set; }                      //displays extra info if true
        public bool EnableSafeMode { get; set; }                       //if true, when reading settings if a value is outside predetermined range it snaps to closest acceptable value
        public bool EnableErrorMode { get; set; }                      //TODO: if true prints all errors of values outside saferanges to the console and closes window on input
        public bool EnableControlLock { get; set; }                    //if true, all controls except debug mode, exit, and unlock are ignored
        public bool ShowGlobalControls { get; set; }                   //shows global controls in debug mode
        public VisualizationMode DefaultMode { get; set; }             //which visualization to start with
        public int ConsoleInstances { get; set; }                      //how many independent window processes to launch
        public bool SaveOnExit { get; set; }                           //if true, saves settings file on program exit
        public bool EnableRasterOnDirectWrite { get; set; }            //if true, switches to raster font when using DirectWrite
        public bool EnableExclusiveMode { get; set; }                  //full screen mode, not taskbar and all that
        public GlobalSettings()
        {
            Restore();
        }

        public void Restore()
        {
            BypassStartupScreen = false;
            ForceDefaultSettings = false;
            EnableDebugMode = true;
            EnableSafeMode = true;
            EnableControlLock = false;
            ShowGlobalControls = true;
            DefaultMode = VisualizationMode.Rings;
            ConsoleInstances = 1;
            SaveOnExit = true;
            EnableRasterOnDirectWrite = true;
            EnableExclusiveMode = false;
        }

        public void EnforceConstraints()
        {
            if (ConsoleInstances > 10) ConsoleInstances = 10;
        }

        public void EnforceMandatoryConstraints()
        {
            if(ConsoleInstances < 1) ConsoleInstances = 1;
            if ((int)DefaultMode < 0 || (int)DefaultMode > Utility.EnumCount<VisualizationMode>(true))
                DefaultMode = VisualizationMode.Rings;
        }
    }
}

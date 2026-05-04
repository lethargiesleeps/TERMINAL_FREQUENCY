using System.Runtime.InteropServices;
using TERMINAL_FREQUENCY.Core;

#nullable disable warnings
namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class ConsoleSettings : IConfigurable
    {
        public ConsoleColor BackgroundColor { get; set; }              //TODO: bg color of console at launch, not implemented
        public bool DisableCursor { get; set; }                        //if true, console cursor is disabled
        public bool DisableAppTitle { get; set; }                      //if true, removes any text to be found on control bar of console window
        public string CustomTitle { get; set; }                        //replace TERMINAL FREQUENCY window title, if empty uses default
        public bool DisableTitleBar { get; set; }                      //removes the entire window control bar if true
        public bool DisableScrollBars { get; set; }                    //removes the x,y scroll bars in the console
        public bool DisableWindowResize { get; set; }                  //if true, window cannot be resized
        public bool LaunchMaximized { get; set; }                      //if true, launches console in maximized mode
        public bool LaunchInCenter { get; set; }                       //if true, launches console in center of screen, gets ignored if launch full screen is true
        public bool LaunchAt { get; set; }                             //if true launches window at LAUNCH_AT_X and LAUNCH_AT_Y
        public int LaunchAtX { get; set; }                             //xPos of where console launches. if -1, gets ignored regardless status of LAUNCH_AT
        public int LaunchAtY { get; set; }                             //yPos of where console launches. if -1, gets ignored regardless status of LAUNCH_AT
        public bool EnableCustomWindowSize { get; set; }               //if true launches with CUSTOM_WINDOW_SIZE_H/W
        public int CustomWindowWidth { get; set; }                     //if true launches with CUSTOM_WINDOW_SIZE_H/W
        public int CustomWindowHeight { get; set; }                    //custom window height if enabled
        public int WindowOpacity { get; set; }                        //window opacity, 0 transparent, 255 solid. BYTE
        public bool AlwaysOnTop { get; set; }                          //if true, console is always on top regardless
        public bool EnableWindowBlur { get; set; }                     //if true, add acryllic blur to the console
        public bool EnableWindowVibrancy { get; set; }                 //if true, does blur but with custom settings, prioritized over blur
        public int WindowVibrancyR { get; set; }                      
        public int WindowVibrancyG { get; set; }
        public int WindowVibrancyB { get; set; }
        public int WindowVibrancyA { get; set; }
        public bool EnableClickThrough { get; set; }                  //if true, can click on apps behind the console. can pair well with opacity and always on top
        public bool EnableFlashOnBeat { get; set; }                   //if true, console flashes in OnSpike hook
        public int FlashOnBeatCount { get; set; }                     //how many time it flashes
        public ConsoleColor[] DefaultColors { get; set; }
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        public ConsoleSettings()
        {
            Restore();
        }

        public void Restore()
        {
            BackgroundColor = ConsoleColor.Black;
            DisableCursor = true;
            DisableAppTitle = false;
            CustomTitle = "";
            DisableTitleBar = false;
            DisableScrollBars = false;
            DisableWindowResize = false;
            LaunchMaximized = false;
            LaunchInCenter = false;
            LaunchAt = false;
            LaunchAtX = -1;
            LaunchAtY = -1;
            EnableCustomWindowSize = false;
            CustomWindowWidth = 115;
            CustomWindowHeight = 35;
            WindowOpacity = 255;
            AlwaysOnTop = false;
            EnableWindowBlur = false;
            EnableWindowVibrancy = false;
            WindowVibrancyR = 0;
            WindowVibrancyG = 0;
            WindowVibrancyB = 0;
            WindowVibrancyA = 0;
            EnableClickThrough = false;
            EnableFlashOnBeat = false;
            FlashOnBeatCount = 2;
            DefaultColors = new[]
            {
                ConsoleColor.White, ConsoleColor.Red, ConsoleColor.Green,
                ConsoleColor.Blue, ConsoleColor.Yellow, ConsoleColor.Cyan,
                ConsoleColor.Magenta, ConsoleColor.Gray, ConsoleColor.DarkRed,
                ConsoleColor.DarkGreen, ConsoleColor.DarkBlue, ConsoleColor.DarkYellow,
                ConsoleColor.DarkCyan, ConsoleColor.DarkMagenta, ConsoleColor.DarkGray
            };
        }

        public void EnforceConstraints()
        {
            if (FlashOnBeatCount > 10 && EnableFlashOnBeat) FlashOnBeatCount = 10;
        }

        public void EnforceMandatoryConstraints()
        {
            if ((int)BackgroundColor < 0 || (int)BackgroundColor > Utility.EnumCount<ConsoleColor>(true))
                BackgroundColor = ConsoleColor.Black;

            if(LaunchAt)
            {
                int displayWidth = GetSystemMetrics(SM_CXSCREEN);
                int displayHeight = GetSystemMetrics(SM_CYSCREEN);

                if (LaunchAtX < -1) LaunchAtX = -1;
                if (LaunchAtX > displayWidth - 50) LaunchAtX = displayWidth - 50;
                if (LaunchAtY < -1) LaunchAtY = -1;
                if (LaunchAtY > displayHeight - 50) LaunchAtY = displayHeight - 50;
            }

            if(EnableCustomWindowSize)
            {
                if (CustomWindowWidth <= 10) CustomWindowWidth = 10;
                if (CustomWindowWidth >= Console.LargestWindowWidth) CustomWindowWidth = Console.LargestWindowWidth;

                if (CustomWindowHeight <= 10) CustomWindowHeight = 10;
                if (CustomWindowHeight >= Console.LargestWindowHeight) CustomWindowHeight = Console.LargestWindowHeight;
            }

            WindowOpacity = Utility.ByteConstraintsCheck(WindowOpacity);

            if (EnableWindowVibrancy)
            {
                WindowVibrancyR = Utility.ByteConstraintsCheck(WindowVibrancyR);
                WindowVibrancyG = Utility.ByteConstraintsCheck(WindowVibrancyG);
                WindowVibrancyB = Utility.ByteConstraintsCheck(WindowVibrancyB);
                WindowVibrancyA = Utility.ByteConstraintsCheck(WindowVibrancyA);
            }

            if (FlashOnBeatCount < 1 && EnableFlashOnBeat) FlashOnBeatCount = 1;
        }
    }
}

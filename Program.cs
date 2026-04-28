using System;
using System.Threading;
using TERMINAL_FREQUENCY.Config;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Visualization;

namespace TERMINAL_FREQUENCY
{
    class Program
    {
        private static bool _isPaused = false;
        private static int _currentMode = Config.Config.DEFAULT_MODE;
        private static List<IVisualization> _visualizations;
        private static IVisualization _currentVisualization;

        static void Main(string[] args)
        {
            bool isChild = args.Length > 0 && args[0] == "--child";

            if (!isChild)
            {
                for (int i = 1; i < Config.Config.INSTANCES; i++)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = Environment.ProcessPath,
                        Arguments = "--child",
                        UseShellExecute = true,
                        CreateNoWindow = false
                    });
                    Thread.Sleep(300);
                }
            }

            // Rest of your code...
            Console.Title = isChild ? $"TERMINAL FREQUENCY - Child" : "TERMINAL FREQUENCY";
            Console.CursorVisible = false;

            Utility.PrintStartup();

            try
            {
                _visualizations = new List<IVisualization>()
                {
                    new Rings(),
                    new Waterfall()
                };

                AudioCapture audioCapture = Config.Config.SPECIFY_AUDIO_DEVICE ? Utility.SelectAudioDevice() : new AudioCapture();

                if (audioCapture == null)
                {
                    Console.WriteLine("\nNo audio device selected. Exiting...");
                    Console.ReadKey();
                    return;
                }


                ScreenBuffer buffer = new ScreenBuffer();
                _currentVisualization = _visualizations[_currentMode];

                //register audio events
                audioCapture.OnVolumeUpdated += (volume) =>
                {
                    if (!_isPaused) _currentVisualization.Update(volume);
                };

                audioCapture.OnVolumeSpike += (volume) =>
                {
                    if (_isPaused) return;

                    if (_currentVisualization is Rings rings)
                        rings.OnSpike();
                    else if(_currentVisualization is Waterfall waterfall)
                        waterfall.OnSpike(volume);
                };

                //capture the audio
                audioCapture.Start();

                //render
                while (true)
                {
                    HandleInput(audioCapture);
                    if(!_isPaused)
                    {
                        _currentVisualization = _visualizations[_currentMode];

                        //redraw
                        buffer.Clear();
                        _currentVisualization.Draw(buffer);

                        //debug bar
                        if(Config.Config.DEBUG_MODE)
                        {
                            string modeName = Utility.GetModeName(_currentMode);
                            string status = $"MODE:{modeName} | VOL:{audioCapture.SmoothedVolume:F2} | PEAK:{audioCapture.PeakVolume:F2} | {(_isPaused ? "PAUSED" : "RUNNING")} | [TAB] Switch Mode | [SPACE] Pause | [ESC] Exit ";

                            if (_currentVisualization is Waterfall)
                                status += $"| [R] Rainbow:{(Config.Config.WATERFALL_RAINBOW_MODE ? "ON" : "OFF")} | [M] Mode:{Config.Config.WATERFALL_MODE} | [X] Reverse:{(Config.Config.WATERFALL_REVERSE_MODE ? "ON" : "OFF")} ";


                            buffer.DrawStatusBar(status, audioCapture.SmoothedVolume);
                        }


                        buffer.Render();
                        Thread.Sleep(Config.Config.THREAD_RATE);
                    }
                    else
                    {
                        buffer.Clear();

                        //debug bar
                        if(Config.Config.DEBUG_MODE)
                        {
                            string pausedStatus = $"PAUSED | MODE:{Utility.GetModeName(_currentMode)} | [SPACE] Resume | [ESC] Exit ";
                            buffer.DrawStatusBar(pausedStatus, 0);
                        }
                        buffer.Render();
                    }

                }


            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Clear();
                Console.WriteLine($"\nERROR: {ex.Message}");
                Console.WriteLine($"\nStack Trace: {ex.StackTrace}");
                Console.ResetColor();
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
            finally
            {
                Console.Clear();
                Console.CursorVisible = true;
                Console.ResetColor();
            }

        }
        static void HandleInput(AudioCapture audioCapture)
        {
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.Escape:
                        audioCapture?.Stop();
                        Environment.Exit(0);
                        break;

                    case ConsoleKey.Spacebar:
                        _isPaused = !_isPaused;
                        break;

                    case ConsoleKey.Tab:
                        if (!_isPaused)
                        {
                            _currentMode = (_currentMode + 1) % _visualizations.Count;
                        }
                        break;

                    case ConsoleKey.R:
                        if (_currentVisualization is Waterfall)
                            Config.Config.WATERFALL_RAINBOW_MODE = !Config.Config.WATERFALL_RAINBOW_MODE;
                        break;

                    case ConsoleKey.M:
                        if (_currentVisualization is Waterfall)
                        {
                            int modeCount = Enum.GetValues(typeof(WaterfallMode)).Length;
                            Config.Config.WATERFALL_MODE = (WaterfallMode)(((int)Config.Config.WATERFALL_MODE + 1) % modeCount);
                        }

                        break;

                    case ConsoleKey.X:
                        if(_currentVisualization is Waterfall)
                            Config.Config.WATERFALL_REVERSE_MODE = !Config.Config.WATERFALL_REVERSE_MODE;
                        break;
                }
            }
        }
    }
}
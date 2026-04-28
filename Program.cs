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
                            string status = $"MODE:{modeName} | VOL:{audioCapture.SmoothedVolume:F2} | PEAK:{audioCapture.PeakVolume:F2} | {(_isPaused ? "PAUSED" : "RUNNING")} | [TAB] Switch | [SPACE] Pause | [ESC] Exit ";
                            buffer.DrawString(0, buffer.Height - 2, status, ConsoleColor.Gray);

                            if (_currentVisualization is Rings)
                            {
                                string ringsStatus = $"RINGS: Re[V]erse:{(Config.Config.RINGS_REVERSE_MODE ? "ON" : "OFF")} | [C]olor:{Config.Config.RING_COLOR_MODE} | Rando[M] Chars:{(Config.Config.RING_CHAR_RANDOMIZER ? "ON" : "OFF")} | [- or =] -/+ MaxR:{Config.Config.RING_RADIUS_MAX} | [O or P] -/+ Segments:{Config.Config.RING_SEGMENTS}";
                                buffer.DrawString(0, buffer.Height - 1, ringsStatus, ConsoleColor.Gray);
                            }

                            if (_currentVisualization is Waterfall)
                            {
                                string waterfallStatus = $"WTRFALL: [R]ainbow:{(Config.Config.WATERFALL_RAINBOW_MODE ? "ON" : "OFF")} | [M]ode:{Config.Config.WATERFALL_MODE} | Re[V]erse:{(Config.Config.WATERFALL_REVERSE_MODE ? "ON" : "OFF")}";

                                if (!Config.Config.WATERFALL_RAINBOW_MODE)
                                    waterfallStatus += $" | [C]olor:{Config.Config.WATERFALL_COLOR}";

                                if (Config.Config.WATERFALL_MODE == WaterfallMode.Normal)
                                    waterfallStatus += $" | [O]rigin:{Config.Config.WATERFALL_ORIGIN}";

                                buffer.DrawString(0, buffer.Height - 1, waterfallStatus, ConsoleColor.Gray);
                            }
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
                        if(!_isPaused)
                            _currentMode = (_currentMode + 1) % _visualizations.Count;
                        break;

                    case ConsoleKey.R:
                        if(_isPaused) return;

                        if(_currentVisualization is Waterfall)
                            Config.Config.WATERFALL_RAINBOW_MODE = !Config.Config.WATERFALL_RAINBOW_MODE;
                        break;

                    case ConsoleKey.M:
                        if(_isPaused) return;

                        if(_currentVisualization is Rings)
                            Config.Config.RING_CHAR_RANDOMIZER = !Config.Config.RING_CHAR_RANDOMIZER;

                        if(_currentVisualization is Waterfall)
                        {
                            int modeCount = Enum.GetValues(typeof(WaterfallMode)).Length;
                            Config.Config.WATERFALL_MODE = (WaterfallMode)(((int)Config.Config.WATERFALL_MODE + 1) % modeCount);
                        }
                        break;

                    case ConsoleKey.V:
                        if(_isPaused) return;

                        if(_currentVisualization is Rings)
                            Config.Config.RINGS_REVERSE_MODE = !Config.Config.RINGS_REVERSE_MODE;

                        if(_currentVisualization is Waterfall)
                            Config.Config.WATERFALL_REVERSE_MODE = !Config.Config.WATERFALL_REVERSE_MODE;
                        break;

                    case ConsoleKey.C:
                        if(_isPaused) return;

                        if(_currentVisualization is Rings)
                        {
                            ColorMode[] cycle = { ColorMode.Light, ColorMode.Red, ColorMode.Green, ColorMode.Blue, ColorMode.Yellow, ColorMode.RainbowLight, ColorMode.RainbowDark };
                            int index = Array.IndexOf(cycle, Config.Config.RING_COLOR_MODE);
                            if (index < 0) index = 0;
                            index = (index + 1) % cycle.Length;
                            Config.Config.RING_COLOR_MODE = cycle[index];
                        }

                        if(_currentVisualization is Waterfall && !Config.Config.WATERFALL_RAINBOW_MODE)
                        {
                            ConsoleColor[] cycle = { ConsoleColor.Gray, ConsoleColor.Red, ConsoleColor.Magenta, ConsoleColor.Blue, ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Green };
                            int index = Array.IndexOf(cycle, Config.Config.WATERFALL_COLOR);
                            if (index < 0) index = 0;
                            index = (index + 1) % cycle.Length;
                            Config.Config.WATERFALL_COLOR = cycle[index];
                        }
                        break;

                    case ConsoleKey.O:
                        if (_currentVisualization is Rings)
                        {
                            Config.Config.RING_SEGMENTS = Math.Max(8, Config.Config.RING_SEGMENTS - 2);
                            Config.Config.RING_AMBIENT_SEGMENTS = Math.Max(8, Config.Config.RING_AMBIENT_SEGMENTS - 2);
                        }

                        if (_currentVisualization is Waterfall && Config.Config.WATERFALL_MODE == WaterfallMode.Normal)
                        {
                            VisualizationOrigin[] cycle = { VisualizationOrigin.Top, VisualizationOrigin.Right, VisualizationOrigin.Bottom, VisualizationOrigin.Left };
                            int index = Array.IndexOf(cycle, Config.Config.WATERFALL_ORIGIN);
                            if (index < 0) index = 0;
                            index = (index + 1) % cycle.Length;
                            Config.Config.WATERFALL_ORIGIN = cycle[index];
                        }
                        break;

                    case ConsoleKey.P:
                        if (_currentVisualization is Rings)
                        {
                            Config.Config.RING_SEGMENTS = Math.Min(60, Config.Config.RING_SEGMENTS + 2);
                            Config.Config.RING_AMBIENT_SEGMENTS = Math.Min(40, Config.Config.RING_AMBIENT_SEGMENTS + 2);
                        }
                        break;

                    case ConsoleKey.OemMinus:
                        if (_currentVisualization is Rings)
                            Config.Config.RING_RADIUS_MAX = Math.Max(Config.Config.RING_RADIUS_MIN + 5, Config.Config.RING_RADIUS_MAX - 5);
                        break;

                    case ConsoleKey.OemPlus:
                        if (_currentVisualization is Rings)
                            Config.Config.RING_RADIUS_MAX = Math.Min(200, Config.Config.RING_RADIUS_MAX + 5);
                        break;
                }
            }
        }
    }
}
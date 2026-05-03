#nullable disable warnings
using System;
using System.Diagnostics;
using System.Threading;
using TERMINAL_FREQUENCY.Config;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Core.CLI;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Visualization.Rings;
using TERMINAL_FREQUENCY.Visualization.Shape;
using TERMINAL_FREQUENCY.Visualization.Waterfall;

namespace TERMINAL_FREQUENCY
{
    class Program
    {
        private static bool _isPaused = false;
        private static int _currentMode = Config.Config.DEFAULT_MODE;
        private static bool _isChild = false;
        private static List<IVisualization> _visualizations;
        private static IVisualization _currentVisualization;
        private static readonly ConsoleColor[] _colors = Config.Config.DEFAULT_COLORS;

        //fps calculations
        private static Stopwatch _stopWatch;
        private static long _sampleWindowStart = 0;
        private static int _framesInWindow = 0;
        private static int _hitsInWindow = 0;
        private static float _currentFps = 0;
        private static float _hitRate = 0;
        private static int _frameCount = 0;
        private const float SAMPLE_DURATION_SECONDS = 1.0f;

        static void Main(string[] args)
        {
            if (Config.Config.ENABLE_THREAD_PRIORITY) Thread.CurrentThread.Priority = Config.Config.THREAD_PRIORITY;
            ConsoleWindow.SetScreenSize(115, 35); //always launch at these defaults

            CLI.HandleCliArgs(args);

            //TODO: Handle Dark Mode better
            if(!Config.Config.DARK_MODE)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Config.Config.SHAPE_UNIFORM_COLOR = ConsoleColor.Black;
                Config.Config.RING_COLOR_MODE = RingColorMode.Dark;
            }


            HandleConsoleWindow();

            try
            {
                _visualizations = new List<IVisualization>()
                {
                    new Rings(),
                    new Waterfall(),
                    new Shape()
                };

                AudioCapture? audioCapture = Config.Config.SPECIFY_AUDIO_DEVICE ? Utility.SelectAudioDevice() : new AudioCapture();

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

                    if (Config.Config.ENABLE_FLASH_ON_BEAT) //doesnt seem to really work
                        ConsoleWindow.FlashWindowOnBeat(Config.Config.FLASH_ON_BEAT_COUNT);

                    if (_currentVisualization is Rings rings)
                        rings.OnSpike();
                    else if(_currentVisualization is Waterfall waterfall)
                        waterfall.OnSpike(volume);
                    else if(_currentVisualization is Shape shape)
                        shape.OnSpike();
                };

                //capture the audio
                audioCapture.Start();

                _stopWatch = Stopwatch.StartNew(); //prep for FPS tracking
                _sampleWindowStart = _stopWatch.ElapsedTicks;

                //render
                while (true)
                {
                    long frameStart = _stopWatch.ElapsedTicks;

                    HandleInput(audioCapture, buffer);

                    if(!_isPaused)
                    {
                        if(Config.Config.DEBUG_MODE)
                        {

                            _framesInWindow++;

                            float elapsedSinceSample = (_stopWatch.ElapsedTicks - _sampleWindowStart) / (float)Stopwatch.Frequency;
                            if (elapsedSinceSample >= SAMPLE_DURATION_SECONDS && _framesInWindow > 0)
                            {
                                _currentFps = _framesInWindow / elapsedSinceSample;

                                //hit rate: was the average FPS over this second on target?
                                //TODO: Fix the hit ratio calc
                                /**
                                if (_currentFps >= Config.Config.TARGET_FPS * 0.98f)
                                    _hitsInWindow++;

                                _hitRate = (float)_hitsInWindow / _framesInWindow * 100f;
                                */
                                _sampleWindowStart = _stopWatch.ElapsedTicks;
                                _framesInWindow = 0;
                            }
                        }

                        _currentVisualization = _visualizations[_currentMode];

                        //redraw
                        buffer.Clear();
                        _currentVisualization.Draw(buffer);

                        //debug bar
                        if(Config.Config.DEBUG_MODE)
                        {

                            if (_currentVisualization is Rings)
                            {
                                string ringsStatus = $"RE[V]ERSE:{(Config.Config.RINGS_REVERSE_MODE ? "ON" : "OFF")} | [C]OLOR:{Utility.FormatEnum(Config.Config.RING_COLOR_MODE)} | RANDO[M] CHARS:{(Config.Config.RING_CHAR_RANDOMIZER ? "ON" : "OFF")} | [-/=] RADIUS:{Config.Config.RING_RADIUS_MAX} | [O/P] SEGMENTS:{Config.Config.RING_SEGMENTS}";
                                buffer.DrawString(0, buffer.Height - 3, ringsStatus, ConsoleColor.Gray);
                            }

                            if (_currentVisualization is Waterfall)
                            {
                                string waterfallStatus = $"[R]AINBOW:{(Config.Config.WATERFALL_RAINBOW_MODE ? "ON" : "OFF")} | [M]ODE:{Utility.FormatEnum(Config.Config.WATERFALL_MODE)} | RE[V]ERSE:{(Config.Config.WATERFALL_REVERSE_MODE ? "ON" : "OFF")}";

                                if (!Config.Config.WATERFALL_RAINBOW_MODE)
                                    waterfallStatus += $" | [C]OLOR:{Utility.FormatEnum(Config.Config.WATERFALL_COLOR)}";

                                if (Config.Config.WATERFALL_MODE == WaterfallMode.Normal)
                                    waterfallStatus += $" | [O]RIGIN:{Utility.FormatEnum(Config.Config.WATERFALL_ORIGIN)}";

                                buffer.DrawString(0, buffer.Height - 3, waterfallStatus, ConsoleColor.Gray);
                            }

                            if(_currentVisualization is Shape)
                            {
                                string shapeStatus = $"[S]HAPE:{Utility.FormatEnum(Config.Config.SHAPE_TYPE)} | LA[Y]OUT:{Utility.FormatEnum(Config.Config.SHAPE_LAYOUT)} | [C]OLOR:{Utility.FormatEnum(Config.Config.SHAPE_UNIFORM_COLOR)} | [F]ILL:{(Config.Config.SHAPE_FILL_MODE ? "ON" : "OFF")} | RE[V]ERSE:{(Config.Config.SHAPE_REVERSE_MODE ? "ON" : "OFF")} | SMOO[T]H:{(Config.Config.SHAPE_SMOOTH_MODE ? "ON" : "OFF")} | [-/=] SIZE:{Config.Config.SHAPE_MAX_SIZE_PERCENT:F2}";

                                if (Config.Config.SHAPE_TYPE == ShapeType.Polygon)
                                    shapeStatus += $" | [9/0] VERT:{Config.Config.SHAPE_POLYGON_SIDES}";
                                if(Config.Config.SHAPE_LAYOUT != ShapeLayout.Single)
                                    shapeStatus += $" | [O/P] COUNT:{Config.Config.SHAPE_COUNT}";
                                buffer.DrawString(0, buffer.Height - 3, shapeStatus, ConsoleColor.Gray);
                            }

                            string modeName = Utility.GetModeName(_currentMode);
                            string status = $"MODE: {modeName} | VOL: {audioCapture.SmoothedVolume:F2} | PEAK: {audioCapture.PeakVolume:F2} | LOCK: {(Config.Config.LOCK_CONTROLS ? "ON" : "OFF")}";
                            buffer.DrawString(0, buffer.Height - 2, status, ConsoleColor.Gray);

                            if (Config.Config.SHOW_GLOBAL_CONTROLS)
                            {
                                string controls = "[TAB] MODE | [SPACE] PAUSE | [D]EBUG | [L]OCK | [ESC] EXIT";
                                buffer.DrawString(0, buffer.Height - 1, controls, ConsoleColor.DarkGray);
                            }

                            //fps stuff
                            int rightX = buffer.Width - 10; //top right corner
                            buffer.DrawString(rightX, 0, $"FPS:{_currentFps,6:F1}", ConsoleColor.Yellow);
                            //buffer.DrawString(rightX, 1, $"TGT:{Config.Config.TARGET_FPS,6}", ConsoleColor.DarkGray);
                            //buffer.DrawString(rightX, 2, $"HIT:{_hitRate,5:F0}%", _hitRate > 90 ? ConsoleColor.Green : _hitRate > 70 ? ConsoleColor.Yellow : ConsoleColor.Red);
                        }

                        buffer.Render();

                        //yield settings
                        long targetTicks = Stopwatch.Frequency / Config.Config.TARGET_FPS;

                        if(Config.Config.ENABLE_YIELD)
                            Thread.Sleep(Config.Config.YIELD_TIMEOUT);
                        else if(Config.Config.ENABLE_SPIN_WAIT)
                        {
                            while (_stopWatch.ElapsedTicks - frameStart < targetTicks)
                                Thread.SpinWait(Config.Config.SPIN_WAIT_ITERATIONS);
                        }
                    }
                    else
                    {
                        buffer.Clear();
                        Utility.PrintPause(buffer, Utility.GetModeName(_currentMode));
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

        static void HandleInput(AudioCapture audioCapture, ScreenBuffer buffer)
        {
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    #region GlobalInputs
                    case ConsoleKey.Escape:
                        audioCapture?.Stop();
                        Environment.Exit(0);
                        break;

                    case ConsoleKey.Spacebar:
                        if (Config.Config.LOCK_CONTROLS) return;
                        _isPaused = !_isPaused;
                        break;

                    case ConsoleKey.Tab:
                        if (Config.Config.LOCK_CONTROLS) return;
                        if(!_isPaused)
                            _currentMode = (_currentMode + 1) % _visualizations.Count; //TODO: Make visualization enum
                        break;

                    case ConsoleKey.D:
                        if(!_isPaused)
                            Config.Config.DEBUG_MODE = !Config.Config.DEBUG_MODE;
                        break;

                    case ConsoleKey.L:
                        if (!_isPaused)
                            Config.Config.LOCK_CONTROLS = !Config.Config.LOCK_CONTROLS;
                        break;
                    #endregion

                    case ConsoleKey.R:
                        if(_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if(_currentVisualization is Waterfall)
                            Config.Config.WATERFALL_RAINBOW_MODE = !Config.Config.WATERFALL_RAINBOW_MODE;
                        break;

                    case ConsoleKey.M:
                        if(_isPaused)
                        {
                            buffer.CycleRenderMode();
                            return;
                        }

                        if (Config.Config.LOCK_CONTROLS) return;

                        if(_currentVisualization is Rings)
                            Config.Config.RING_CHAR_RANDOMIZER = !Config.Config.RING_CHAR_RANDOMIZER;

                        if(_currentVisualization is Waterfall)
                            Config.Config.WATERFALL_MODE = Utility.CycleNextEnum(Config.Config.WATERFALL_MODE);
                        break;

                    case ConsoleKey.V:
                        if(_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if(_currentVisualization is Rings)
                            Config.Config.RINGS_REVERSE_MODE = !Config.Config.RINGS_REVERSE_MODE;

                        if(_currentVisualization is Waterfall)
                            Config.Config.WATERFALL_REVERSE_MODE = !Config.Config.WATERFALL_REVERSE_MODE;

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_REVERSE_MODE = !Config.Config.SHAPE_REVERSE_MODE;
                        break;

                    case ConsoleKey.C:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Rings)
                        {
                            RingColorMode[] cycle = { RingColorMode.Light, RingColorMode.Red, RingColorMode.Green, RingColorMode.Blue, RingColorMode.Yellow, RingColorMode.RainbowLight, RingColorMode.RainbowDark };
                            Config.Config.RING_COLOR_MODE = Utility.CycleNext(cycle, Config.Config.RING_COLOR_MODE);
                        }

                        if(_currentVisualization is Waterfall && !Config.Config.WATERFALL_RAINBOW_MODE)
                            Config.Config.WATERFALL_COLOR = Utility.CycleNext(_colors, Config.Config.WATERFALL_COLOR);


                        if(_currentVisualization is Shape)
                            Config.Config.SHAPE_UNIFORM_COLOR = Utility.CycleNext(_colors, Config.Config.SHAPE_UNIFORM_COLOR);
                        break;

                    case ConsoleKey.F:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_FILL_MODE = !Config.Config.SHAPE_FILL_MODE;
                        break;

                    case ConsoleKey.S:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if(_currentVisualization is Shape)
                            Config.Config.SHAPE_TYPE = Utility.CycleNextEnum(Config.Config.SHAPE_TYPE);
                        break;

                    case ConsoleKey.Y:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_LAYOUT = Utility.CycleNextEnum(Config.Config.SHAPE_LAYOUT);
                        break;

                    case ConsoleKey.T:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_SMOOTH_MODE = !Config.Config.SHAPE_SMOOTH_MODE;
                        break;

                    case ConsoleKey.O:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Rings)
                        {
                            Config.Config.RING_SEGMENTS = Math.Max(8, Config.Config.RING_SEGMENTS - 2);
                            Config.Config.RING_AMBIENT_SEGMENTS = Math.Max(8, Config.Config.RING_AMBIENT_SEGMENTS - 2);
                        }

                        if (_currentVisualization is Waterfall && Config.Config.WATERFALL_MODE == WaterfallMode.Normal)
                        {
                            VisualizationOrigin[] cycle = { VisualizationOrigin.Top, VisualizationOrigin.Right, VisualizationOrigin.Bottom, VisualizationOrigin.Left };
                            Config.Config.WATERFALL_ORIGIN = Utility.CycleNext(cycle, Config.Config.WATERFALL_ORIGIN);
                        }

                        if (_currentVisualization is Shape)
                        {
                            if (Config.Config.SHAPE_LAYOUT == ShapeLayout.Single) return;

                            int shapeCount = Math.Max(1, Config.Config.SHAPE_COUNT - 1);
                            Config.Config.SHAPE_COUNT = shapeCount;
                        }
                        break;

                    case ConsoleKey.P:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Rings)
                        {
                            Config.Config.RING_SEGMENTS = Math.Min(100, Config.Config.RING_SEGMENTS + 2);
                            Config.Config.RING_AMBIENT_SEGMENTS = Math.Min(80, Config.Config.RING_AMBIENT_SEGMENTS + 2);
                        }

                        if (_currentVisualization is Shape)
                        {
                            if (Config.Config.SHAPE_LAYOUT == ShapeLayout.Single) return;

                            int shapeCount = Math.Min(4, Config.Config.SHAPE_COUNT + 1);
                            Config.Config.SHAPE_COUNT = shapeCount;
                        }
                        break;

                    case ConsoleKey.OemMinus:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Rings)
                            Config.Config.RING_RADIUS_MAX = Math.Max(Config.Config.RING_RADIUS_MIN + 5, Config.Config.RING_RADIUS_MAX - 5);

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_MAX_SIZE_PERCENT = Math.Max(0.05f, Config.Config.SHAPE_MAX_SIZE_PERCENT - 0.02f);

                        break;

                    case ConsoleKey.OemPlus:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Rings)
                            Config.Config.RING_RADIUS_MAX = Math.Min(200, Config.Config.RING_RADIUS_MAX + 5);

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_MAX_SIZE_PERCENT = Math.Min(1.0f, Config.Config.SHAPE_MAX_SIZE_PERCENT + 0.02f);
                        break;

                    case ConsoleKey.D9:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Shape)
                        {
                            if (Config.Config.SHAPE_TYPE != ShapeType.Polygon) return;

                            int[] validSides = { 5, 6, 8, 10, 12 };

                            Config.Config.SHAPE_POLYGON_SIDES = Utility.CyclePrevious(validSides, Config.Config.SHAPE_POLYGON_SIDES, true);
                        }
                        break;

                    case ConsoleKey.D0:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Shape)
                        {
                            if (Config.Config.SHAPE_TYPE != ShapeType.Polygon) return;

                            int[] validSides = { 5, 6, 8, 10, 12 };

                            Config.Config.SHAPE_POLYGON_SIDES = Utility.CycleNext(validSides, Config.Config.SHAPE_POLYGON_SIDES, true);
                        }
                        break;
                }
            }
        }
        static void HandleConsoleWindow()
        {
            //manage window features
            if (Config.Config.DISABLE_TITLE_BAR)
                ConsoleWindow.DisableTitleBar(); //TODO: still see a bit of border, likely DWM border

            if(Config.Config.DISABLE_SCROLL_BARS)
                ConsoleWindow.DisableScrollBars();



            ConsoleWindow.SetAlwaysOnTop(Config.Config.ALWAYS_ON_TOP);
            ConsoleWindow.SetOpacity(Config.Config.WINDOW_OPACITY);
            ConsoleWindow.SetClickThrough(Config.Config.ENABLE_CLICK_THROUGH);

            if (Config.Config.ENABLE_WINDOW_VIBRANCY)
                ConsoleWindow.SetWindowVibrancy(Config.Config.WINDOW_VIBRANCY_R, Config.Config.WINDOW_VIBRANCY_G, Config.Config.WINDOW_VIBRANCY_B, Config.Config.WINDOW_VIBRANCY_A);
            else if (Config.Config.ENABLE_WINDOW_BLUR)
                ConsoleWindow.SetWindowBlur(Config.Config.ENABLE_WINDOW_BLUR);

            //seems to not work
            if (Config.Config.ENABLE_WINDOW_GLOW)
                ConsoleWindow.SetWindowGlow(Config.Config.WINDOW_GLOW_RADIUS, (byte)Config.Config.WINDOW_GLOW_R, (byte)Config.Config.WINDOW_GLOW_G, (byte)Config.Config.WINDOW_GLOW_B);
            
            //size
            if (Config.Config.LAUNCH_FULL_SCREEN)
                ConsoleWindow.SetFullScreen();
            else if (Config.Config.ENABLE_CUSTOM_WINDOW_SIZE)
                ConsoleWindow.SetScreenSize(Config.Config.CUSTOM_WINDOW_WIDTH, Config.Config.CUSTOM_WINDOW_HEIGHT);

            //position
            if (!Config.Config.LAUNCH_FULL_SCREEN)
            {
                if (Config.Config.LAUNCH_AT && Config.Config.LAUNCH_AT_X >= 0 && Config.Config.LAUNCH_AT_Y >= 0)
                    ConsoleWindow.LaunchConsoleAt(Config.Config.LAUNCH_AT_X, Config.Config.LAUNCH_AT_Y);
                else if (Config.Config.LAUNCH_IN_CENTER)
                    ConsoleWindow.LaunchConsoleCenter();
            }

            if (Config.Config.DISABLE_WINDOW_RESIZE)
                ConsoleWindow.DisableResize();

            Console.CursorVisible = false;

            //manage process title
            if (Config.Config.DISABLE_APP_TITLE)
                Console.Title = string.Empty;
            else if (!string.IsNullOrEmpty(Config.Config.CUSTOM_TITLE))
                Console.Title = Config.Config.CUSTOM_TITLE;
            else
                Console.Title = _isChild ? $"TERMINAL FREQUENCY - Child" : "TERMINAL FREQUENCY";

            if (!Config.Config.BYPASS_STARTUP)
                Utility.PrintStartup();

        }
    }
}
#pragma warning restore CS8618
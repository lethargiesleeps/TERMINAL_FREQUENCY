using System;
using System.Threading;
using TERMINAL_FREQUENCY.Config;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Visualization.Shape;

namespace TERMINAL_FREQUENCY
{
    class Program
    {
        private static bool _isPaused = false;
        private static int _currentMode = Config.Config.DEFAULT_MODE;
        private static List<IVisualization> _visualizations;
        private static IVisualization _currentVisualization;

        private static ConsoleColor[] _colors = Config.Config.DEFAULT_COLORS;


        static void Main(string[] args)
        {
            bool isChild = args.Length > 0 && args[0] == "--child";

            if(!Config.Config.DARK_MODE)
            {
                Console.BackgroundColor = ConsoleColor.White;
                Config.Config.SHAPE_UNIFORM_COLOR = ConsoleColor.Black;
                Config.Config.RING_COLOR_MODE = ColorMode.Dark;
            }
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


            Console.Title = isChild ? $"TERMINAL FREQUENCY - Child" : "TERMINAL FREQUENCY";
            Console.CursorVisible = false;

            Utility.PrintStartup();

            try
            {
                _visualizations = new List<IVisualization>()
                {
                    new Rings(),
                    new Waterfall(),
                    new Shape()
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
                    else if(_currentVisualization is Shape shape)
                        shape.OnSpike();
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
                                string controls = "[TAB] MODE | [SPACE] PAUSE | [D]EBUG | [L]OCK | [1-6] FPS | [ESC] EXIT";
                                buffer.DrawString(0, buffer.Height - 1, controls, ConsoleColor.DarkGray);
                            }
                        }


                        buffer.Render();
                        Thread.Sleep(Config.Config.THREAD_RATE);
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
        static void HandleInput(AudioCapture audioCapture)
        {
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    #region GlobalControls
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
                            _currentMode = (_currentMode + 1) % _visualizations.Count;
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

                    #region VisualizationControls
                    case ConsoleKey.R:
                        if(_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if(_currentVisualization is Waterfall)
                            Config.Config.WATERFALL_RAINBOW_MODE = !Config.Config.WATERFALL_RAINBOW_MODE;
                        break;

                    case ConsoleKey.M:
                        if(_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if(_currentVisualization is Rings)
                            Config.Config.RING_CHAR_RANDOMIZER = !Config.Config.RING_CHAR_RANDOMIZER;

                        if(_currentVisualization is Waterfall)
                        {
                            int modeCount = Enum.GetValues(typeof(WaterfallMode)).Length;
                            Config.Config.WATERFALL_MODE = (WaterfallMode)(((int)Config.Config.WATERFALL_MODE + 1) % modeCount);
                        }
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
                            ColorMode[] cycle = { ColorMode.Light, ColorMode.Red, ColorMode.Green, ColorMode.Blue, ColorMode.Yellow, ColorMode.RainbowLight, ColorMode.RainbowDark };
                            int index = Array.IndexOf(cycle, Config.Config.RING_COLOR_MODE);
                            if (index < 0) index = 0;
                            index = (index + 1) % cycle.Length;
                            Config.Config.RING_COLOR_MODE = cycle[index];
                        }

                        if(_currentVisualization is Waterfall && !Config.Config.WATERFALL_RAINBOW_MODE)
                        {
                            //TODO: make this a utility function
                            int index = Array.IndexOf(_colors, Config.Config.WATERFALL_COLOR);
                            if (index < 0) index = 0;
                            index = (index + 1) % _colors.Length;
                            Config.Config.WATERFALL_COLOR = _colors[index];
                        }

                        if(_currentVisualization is Shape)
                        {
                            int index = Array.IndexOf(_colors, Config.Config.SHAPE_UNIFORM_COLOR);
                            if (index < 0) index = 0;
                            index = (index + 1) % _colors.Length;
                            Config.Config.SHAPE_UNIFORM_COLOR = _colors[index];
                        }
                        break;

                    case ConsoleKey.F:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_FILL_MODE = !Config.Config.SHAPE_FILL_MODE;
                        break;

                    case ConsoleKey.S:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if(_currentVisualization is Shape)
                        {
                            ShapeType[] types = (ShapeType[])Enum.GetValues(typeof(ShapeType));
                            int index = Array.IndexOf(types, Config.Config.SHAPE_TYPE);
                            if (index < 0) index = 0;
                            index = (index + 1) % types.Length;
                            Config.Config.SHAPE_TYPE = types[index];
                        }
                        break;

                    case ConsoleKey.Y:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;
                        if (_currentVisualization is Shape)
                        {
                            ShapeLayout[] layouts = (ShapeLayout[])Enum.GetValues(typeof(ShapeLayout));
                            int index = Array.IndexOf(layouts, Config.Config.SHAPE_LAYOUT);
                            if (index < 0) index = 0;
                            index = (index + 1) % layouts.Length;
                            Config.Config.SHAPE_LAYOUT = layouts[index];
                        }
                        break;

                    case ConsoleKey.T:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_SMOOTH_MODE = !Config.Config.SHAPE_SMOOTH_MODE;

                        break;
                    case ConsoleKey.O: //decrement param 2, except Waterfall Normal mode
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;
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

                        if (_currentVisualization is Shape)
                        {
                            if (Config.Config.SHAPE_LAYOUT == ShapeLayout.Single) return;
                            int shapeCount = Math.Max(1, Config.Config.SHAPE_COUNT - 1);
                            Config.Config.SHAPE_COUNT = shapeCount;
                        }
                        break;

                    case ConsoleKey.P: //increment param 2
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;
                        if (_currentVisualization is Rings)
                        {
                            Config.Config.RING_SEGMENTS = Math.Min(60, Config.Config.RING_SEGMENTS + 2);
                            Config.Config.RING_AMBIENT_SEGMENTS = Math.Min(40, Config.Config.RING_AMBIENT_SEGMENTS + 2);
                        }

                        if (_currentVisualization is Shape)
                        {
                            if (Config.Config.SHAPE_LAYOUT == ShapeLayout.Single) return;
                            int shapeCount = Math.Min(4, Config.Config.SHAPE_COUNT + 1);
                            Config.Config.SHAPE_COUNT = shapeCount;
                        }
                        break;

                    case ConsoleKey.OemMinus: //decrement param 1
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;
                        if (_currentVisualization is Rings)
                            Config.Config.RING_RADIUS_MAX = Math.Max(Config.Config.RING_RADIUS_MIN + 5, Config.Config.RING_RADIUS_MAX - 5);

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_MAX_SIZE_PERCENT = Math.Max(0.05f, Config.Config.SHAPE_MAX_SIZE_PERCENT - 0.02f);

                        break;

                    case ConsoleKey.OemPlus: //increment param 1
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;
                        if (_currentVisualization is Rings)
                            Config.Config.RING_RADIUS_MAX = Math.Min(200, Config.Config.RING_RADIUS_MAX + 5);

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_MAX_SIZE_PERCENT = Math.Min(1.0f, Config.Config.SHAPE_MAX_SIZE_PERCENT + 0.02f);
                        break;
                    #endregion

                    case ConsoleKey.D9:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Shape)
                        {
                            if (Config.Config.SHAPE_TYPE != ShapeType.Polygon) return;
                            int[] validSides = { 5, 6, 8, 10, 12 };
                            int currentIndex = Array.IndexOf(validSides, Config.Config.SHAPE_POLYGON_SIDES);
                            if (currentIndex > 0)
                                Config.Config.SHAPE_POLYGON_SIDES = validSides[currentIndex - 1];
                        }
                        break;

                    case ConsoleKey.D0:
                        if (_isPaused || Config.Config.LOCK_CONTROLS) return;

                        if (_currentVisualization is Shape)
                        {
                            if (Config.Config.SHAPE_TYPE != ShapeType.Polygon) return;
                            int[] validSides = { 5, 6, 8, 10, 12 };
                            int currentIndex = Array.IndexOf(validSides, Config.Config.SHAPE_POLYGON_SIDES);
                            if (currentIndex < validSides.Length - 1)
                                Config.Config.SHAPE_POLYGON_SIDES = validSides[currentIndex + 1];
                        }
                        break;
                }
            }
        }
    }
}
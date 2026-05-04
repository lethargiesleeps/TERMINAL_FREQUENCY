#nullable disable warnings
using System;
using System.Diagnostics;
using System.Threading;
using TERMINAL_FREQUENCY.Config;
using TERMINAL_FREQUENCY.Config.Settings;
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
        private static Settings _settings = new Settings();
        private static bool _isPaused = false;
        private static int _currentMode = (int)_settings.GlobalSettings.DefaultMode;
        private static bool _isChild = false;
        private static List<IVisualization> _visualizations;
        private static IVisualization _currentVisualization;
        private static readonly ConsoleColor[] _colors = _settings.ConsoleSettings.DefaultColors;

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
            if (_settings.RendererSettings.EnableThreadPriority)
                Thread.CurrentThread.Priority = _settings.RendererSettings.ThreadPriority;

            Config.Font.Font.SetCustomFont(Config.Font.FontFace.Consolas, 16, false); //always start at default type
            Config.Font.Font.SaveCurrentFont();

            if(_settings.FontSettings.EnableRasterFont)
                Config.Font.Font.SetRasterFont(_settings.FontSettings.RasterFontType);
            else if(_settings.FontSettings.EnableCustomFont)
                Config.Font.Font.SetCustomFont(
                    _settings.FontSettings.CustomFontFace, 
                    _settings.FontSettings.CustomFontSize, 
                    _settings.FontSettings.CustomFontBold, 
                    _settings.FontSettings.CustomFontFaceOverride
                    );

            ConsoleWindow.SetScreenSize(115, 35); //always launch at these defaults
            CLI.HandleCliArgs(args, _settings.GlobalSettings);

            HandleConsoleWindow();

            try
            {
                _visualizations = new List<IVisualization>()
                {
                    new Rings(_settings),
                    new Waterfall(_settings),
                    new Shape()
                };

                AudioCapture? audioCapture = _settings.AudioCaptureSettings.SpecifyAudioDevice ? Utility.SelectAudioDevice() : new AudioCapture(_settings);

                if (audioCapture == null)
                {
                    Console.WriteLine("\nNo audio device selected. Exiting...");
                    Console.ReadKey();
                    return;
                }


                ScreenBuffer buffer = new ScreenBuffer(_settings);
                _currentVisualization = _visualizations[_currentMode];

                //register audio events
                audioCapture.OnVolumeUpdated += (volume) =>
                {
                    if (!_isPaused) _currentVisualization.Update(volume);
                };

                audioCapture.OnVolumeSpike += (volume) =>
                {
                    if (_isPaused) return;

                    if (_settings.ConsoleSettings.EnableFlashOnBeat)
                        ConsoleWindow.FlashWindowOnBeat(_settings.ConsoleSettings.FlashOnBeatCount);

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
                        if(_settings.GlobalSettings.EnableDebugMode)
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
                        if(_settings.GlobalSettings.EnableDebugMode)
                        {

                            if (_currentVisualization is Rings)
                            {
                                string ringsStatus = $"RE[V]ERSE:{(_settings.RingsSettings.ReverseMode ? "ON" : "OFF")} | [C]OLOR:{Utility.FormatEnum(_settings.RingsSettings.ColorMode)} | RANDO[M] CHARS:{(_settings.RingsSettings.CharRandomizer ? "ON" : "OFF")} | [-/=] RADIUS:{_settings.RingsSettings.RadiusMax} | [O/P] SEGMENTS:{_settings.RingsSettings.Segments}";
                                buffer.DrawString(0, buffer.Height - 3, ringsStatus, ConsoleColor.Gray);
                            }

                            if (_currentVisualization is Waterfall)
                            {
                                string waterfallStatus = $"[R]AINBOW:{(_settings.WaterfallSettings.RainbowMode ? "ON" : "OFF")} | [M]ODE:{Utility.FormatEnum(_settings.WaterfallSettings.Mode)} | RE[V]ERSE:{(_settings.WaterfallSettings.ReverseMode ? "ON" : "OFF")}";

                                if (!_settings.WaterfallSettings.RainbowMode)
                                    waterfallStatus += $" | [C]OLOR:{Utility.FormatEnum(_settings.WaterfallSettings.Color)}";

                                if (_settings.WaterfallSettings.Mode == WaterfallMode.Normal)
                                    waterfallStatus += $" | [O]RIGIN:{Utility.FormatEnum(_settings.WaterfallSettings.Origin)}";

                                buffer.DrawString(0, buffer.Height - 3, waterfallStatus, ConsoleColor.Gray);
                            }

                            if(_currentVisualization is Shape)
                            {
                                string shapeStatus = $"[S]HAPE:{Utility.FormatEnum(Config.Config.SHAPE_TYPE)} | LA[Y]OUT:{Utility.FormatEnum(Config.Config.SHAPE_LAYOUT)} | [C]OLOR:{Utility.FormatEnum(Config.Config.SHAPE_UNIFORM_COLOR)} | [F]ILL:{(Config.Config.SHAPE_FILL_MODE ? "ON" : "OFF")} | RE[V]ERSE:{(Config.Config.SHAPE_REVERSE_MODE ? "ON" : "OFF")} | SMOO[T]H:{(Config.Config.SHAPE_SMOOTH_MODE ? "ON" : "OFF")} | [-/=] SIZE:{Config.Config.SHAPE_MAX_SIZE_PERCENT:F2}";

                                if (Config.Config.SHAPE_TYPE == ShapeType.Polygon)
                                    shapeStatus += $" | [9/0] VERT:{Config.Config.SHAPE_POLYGON_SIDES}";
                                if(Config.Config.SHAPE_LAYOUT != ShapeLayout.Single && Config.Config.SHAPE_LAYOUT != ShapeLayout.Concentric)
                                    shapeStatus += $" | [O/P] COUNT:{Config.Config.SHAPE_COUNT}";
                                if(Config.Config.SHAPE_LAYOUT == ShapeLayout.Concentric)
                                    shapeStatus += $" | [O/P] COUNT:{Config.Config.SHAPE_CONCENTRIC_LAYERS}";

                                buffer.DrawString(0, buffer.Height - 3, shapeStatus, ConsoleColor.Gray);
                            }

                            string modeName = Utility.GetModeName(_currentMode);
                            string status = $"MODE: {modeName} | VOL: {audioCapture.SmoothedVolume:F2} | PEAK: {audioCapture.PeakVolume:F2} | LOCK: {(_settings.GlobalSettings.EnableControlLock ? "ON" : "OFF")}";
                            buffer.DrawString(0, buffer.Height - 2, status, ConsoleColor.Gray);

                            if (_settings.GlobalSettings.ShowGlobalControls)
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
                        long targetTicks = Stopwatch.Frequency / _settings.RendererSettings.TargetFps;

                        if(_settings.RendererSettings.EnableYield)
                            Thread.Sleep(_settings.RendererSettings.YieldTimeout);
                        else if(_settings.RendererSettings.EnableSpinWait)
                        {
                            while (_stopWatch.ElapsedTicks - frameStart < targetTicks)
                                Thread.SpinWait(_settings.RendererSettings.SpinWaitIterations);
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
                        if (_settings.GlobalSettings.EnableControlLock) return;
                        _isPaused = !_isPaused;
                        break;

                    case ConsoleKey.Tab:
                        if (_settings.GlobalSettings.EnableControlLock) return;
                        if(!_isPaused)
                            _currentMode = (_currentMode + 1) % _visualizations.Count; //TODO: Make visualization enum
                        break;

                    case ConsoleKey.D:
                        if(!_isPaused)
                            _settings.GlobalSettings.EnableDebugMode = !_settings.GlobalSettings.EnableDebugMode;
                        break;

                    case ConsoleKey.L:
                        if (!_isPaused)
                            _settings.GlobalSettings.EnableControlLock = !_settings.GlobalSettings.EnableControlLock;
                        break;
                    #endregion

                    case ConsoleKey.R:
                        if(_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if(_currentVisualization is Waterfall)
                            _settings.WaterfallSettings.RainbowMode = !_settings.WaterfallSettings.RainbowMode;
                        break;

                    case ConsoleKey.M:
                        if(_isPaused)
                        {
                            buffer.CycleRenderMode();
                            return;
                        }

                        if (_settings.GlobalSettings.EnableControlLock) return;

                        if(_currentVisualization is Rings)
                            _settings.RingsSettings.CharRandomizer = !_settings.RingsSettings.CharRandomizer;

                        if(_currentVisualization is Waterfall)
                            _settings.WaterfallSettings.Mode = Utility.CycleNextEnum(_settings.WaterfallSettings.Mode);
                        break;

                    case ConsoleKey.V:
                        if(_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                            _settings.RingsSettings.ReverseMode = !_settings.RingsSettings.ReverseMode;

                        if (_currentVisualization is Waterfall)
                            _settings.WaterfallSettings.ReverseMode = !_settings.WaterfallSettings.ReverseMode;

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_REVERSE_MODE = !Config.Config.SHAPE_REVERSE_MODE;
                        break;

                    case ConsoleKey.C:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                        {
                            RingColorMode[] cycle = { RingColorMode.Light, RingColorMode.Red, RingColorMode.Green, RingColorMode.Blue, RingColorMode.Yellow, RingColorMode.RainbowLight, RingColorMode.RainbowDark };
                            _settings.RingsSettings.ColorMode = Utility.CycleNext(cycle, _settings.RingsSettings.ColorMode);
                        }

                        if(_currentVisualization is Waterfall && !_settings.WaterfallSettings.RainbowMode)
                            _settings.WaterfallSettings.Color = Utility.CycleNext(_colors, _settings.WaterfallSettings.Color);


                        if(_currentVisualization is Shape)
                            Config.Config.SHAPE_UNIFORM_COLOR = Utility.CycleNext(_colors, Config.Config.SHAPE_UNIFORM_COLOR);
                        break;

                    case ConsoleKey.F:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_FILL_MODE = !Config.Config.SHAPE_FILL_MODE;
                        break;

                    case ConsoleKey.S:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if(_currentVisualization is Shape)
                            Config.Config.SHAPE_TYPE = Utility.CycleNextEnum(Config.Config.SHAPE_TYPE);
                        break;

                    case ConsoleKey.Y:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_LAYOUT = Utility.CycleNextEnum(Config.Config.SHAPE_LAYOUT);
                        break;

                    case ConsoleKey.T:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_SMOOTH_MODE = !Config.Config.SHAPE_SMOOTH_MODE;
                        break;

                    case ConsoleKey.O:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                        {
                            _settings.RingsSettings.Segments = Math.Max(8, _settings.RingsSettings.Segments - 2);
                            _settings.RingsSettings.AmbientSegments = Math.Max(8, _settings.RingsSettings.AmbientSegments - 2);
                        }

                        if (_currentVisualization is Waterfall && _settings.WaterfallSettings.Mode == WaterfallMode.Normal)
                        {
                            VisualizationOrigin[] cycle = { VisualizationOrigin.Top, VisualizationOrigin.Right, VisualizationOrigin.Bottom, VisualizationOrigin.Left };
                            _settings.WaterfallSettings.Origin = Utility.CycleNext(cycle, _settings.WaterfallSettings.Origin);
                        }

                        if (_currentVisualization is Shape)
                        {
                            if (Config.Config.SHAPE_LAYOUT == ShapeLayout.Single) return;

                            if (Config.Config.SHAPE_LAYOUT == ShapeLayout.Concentric)
                            {
                                int layerCount = Math.Max(1, Config.Config.SHAPE_CONCENTRIC_LAYERS - 1);
                                Config.Config.SHAPE_CONCENTRIC_LAYERS = layerCount;
                                return;
                            }

                            int shapeCount = Math.Max(1, Config.Config.SHAPE_COUNT - 1);
                            Config.Config.SHAPE_COUNT = shapeCount;
                        }
                        break;

                    case ConsoleKey.P:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                        {
                            _settings.RingsSettings.Segments = Math.Min(100, _settings.RingsSettings.Segments + 2);
                            _settings.RingsSettings.AmbientSegments = Math.Min(80, _settings.RingsSettings.AmbientSegments + 2);
                        }

                        if (_currentVisualization is Shape)
                        {
                            if (Config.Config.SHAPE_LAYOUT == ShapeLayout.Single) return;

                            if (Config.Config.SHAPE_LAYOUT == ShapeLayout.Concentric)
                            {
                                int layerCount = Math.Min(10, Config.Config.SHAPE_CONCENTRIC_LAYERS + 1);
                                Config.Config.SHAPE_CONCENTRIC_LAYERS = layerCount;
                                return;
                            }
                            int shapeCount = Math.Min(4, Config.Config.SHAPE_COUNT + 1);
                            Config.Config.SHAPE_COUNT = shapeCount;
                        }
                        break;

                    case ConsoleKey.OemMinus:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                            _settings.RingsSettings.RadiusMax = Math.Max(_settings.RingsSettings.RadiusMin + 5, _settings.RingsSettings.RadiusMax - 5);

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_MAX_SIZE_PERCENT = Math.Max(0.05f, Config.Config.SHAPE_MAX_SIZE_PERCENT - 0.02f);

                        break;

                    case ConsoleKey.OemPlus:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                            _settings.RingsSettings.RadiusMax = Math.Min(200, _settings.RingsSettings.RadiusMax + 5);

                        if (_currentVisualization is Shape)
                            Config.Config.SHAPE_MAX_SIZE_PERCENT = Math.Min(1.0f, Config.Config.SHAPE_MAX_SIZE_PERCENT + 0.02f);
                        break;

                    case ConsoleKey.D9:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Shape)
                        {
                            if (Config.Config.SHAPE_TYPE != ShapeType.Polygon) return;

                            int[] validSides = { 5, 6, 8, 10, 12 };

                            Config.Config.SHAPE_POLYGON_SIDES = Utility.CyclePrevious(validSides, Config.Config.SHAPE_POLYGON_SIDES, true);
                        }
                        break;

                    case ConsoleKey.D0:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

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
            if (_settings.ConsoleSettings.DisableTitleBar)
                ConsoleWindow.DisableTitleBar(); //TODO: still see a bit of border, likely DWM border

            if(_settings.ConsoleSettings.DisableScrollBars)
                ConsoleWindow.DisableScrollBars();



            ConsoleWindow.SetAlwaysOnTop(_settings.ConsoleSettings.AlwaysOnTop);
            ConsoleWindow.SetOpacity((byte)_settings.ConsoleSettings.WindowOpacity);
            ConsoleWindow.SetClickThrough(_settings.ConsoleSettings.EnableClickThrough);

            if (_settings.ConsoleSettings.EnableWindowVibrancy)
                ConsoleWindow.SetWindowVibrancy(
                    (byte)_settings.ConsoleSettings.WindowVibrancyR,
                    (byte)_settings.ConsoleSettings.WindowVibrancyG,
                    (byte)_settings.ConsoleSettings.WindowVibrancyB,
                    (byte)_settings.ConsoleSettings.WindowVibrancyA
                );
            else if (_settings.ConsoleSettings.EnableWindowBlur)
                ConsoleWindow.SetWindowBlur(_settings.ConsoleSettings.EnableWindowBlur);

            //size
            if (_settings.ConsoleSettings.LaunchMaximized)
                ConsoleWindow.SetFullScreen();
            else if (_settings.ConsoleSettings.EnableCustomWindowSize)
                ConsoleWindow.SetScreenSize(_settings.ConsoleSettings.CustomWindowWidth, _settings.ConsoleSettings.CustomWindowHeight);

            //position
            if (!_settings.ConsoleSettings.LaunchMaximized)
            {
                if (_settings.ConsoleSettings.LaunchAt && _settings.ConsoleSettings.LaunchAtX >= 0 && _settings.ConsoleSettings.LaunchAtY >= 0)
                    ConsoleWindow.LaunchConsoleAt(_settings.ConsoleSettings.LaunchAtX, _settings.ConsoleSettings.LaunchAtY);
                else if (_settings.ConsoleSettings.LaunchInCenter)
                    ConsoleWindow.LaunchConsoleCenter();
            }

            if (_settings.ConsoleSettings.DisableWindowResize)
                ConsoleWindow.DisableResize();

            Console.CursorVisible = false;

            //manage process title
            if (_settings.ConsoleSettings.DisableAppTitle)
                Console.Title = string.Empty;
            else if (!string.IsNullOrEmpty(_settings.ConsoleSettings.CustomTitle))
                Console.Title = _settings.ConsoleSettings.CustomTitle;
            else
                Console.Title = _isChild ? $"TERMINAL FREQUENCY - Child" : "TERMINAL FREQUENCY";

            if (!_settings.GlobalSettings.BypassStartupScreen)
                Utility.PrintStartup();

        }
    }
}
#pragma warning restore CS8618
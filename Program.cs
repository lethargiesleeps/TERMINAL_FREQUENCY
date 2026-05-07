#nullable disable warnings
using System;
using System.Diagnostics;
using System.Threading;
using TERMINAL_FREQUENCY.Config;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Core.Audio;
using TERMINAL_FREQUENCY.Core.CLI;
using TERMINAL_FREQUENCY.Core.Rendering;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Visualization.Equalizer;
using TERMINAL_FREQUENCY.Visualization.Rings;
using TERMINAL_FREQUENCY.Visualization.Shape;
using TERMINAL_FREQUENCY.Visualization.Waterfall;

namespace TERMINAL_FREQUENCY
{
    class Program
    {
        private static Settings _settings;
        private static List<IVisualization> _visualizations;
        private static IVisualization _currentVisualization;
        private static ConsoleColor[] _colors = [];
        private static int _currentMode;
        private static bool _exclusiveMode;
        private static bool _isPaused = false;
        private static bool _isDebug = Debugger.IsAttached;
        private static bool _isChild = false;
        private static bool _isSavingOrLoading = false;

        //fps calculations
        private static Stopwatch _stopWatch;
        private static long _sampleWindowStart = 0;
        private static int _framesInWindow = 0;
        private static float _currentFps = 0;

        private const float SAMPLE_DURATION_SECONDS = 1.0f;

        static void Main(string[] args)
        {
            try
            {
                _settings = SettingsManager.Load();
                _settings.EnforceMandatoryConstraints();
                SettingsManager.Save(_settings);
            }
            catch(Exception e)
            {
                _settings = new Settings();

                if(_isDebug)
                {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine(e.GetType());
                    Debug.WriteLine(e.StackTrace);
                }
            }
            finally
            {
                if (_settings.GlobalSettings.EnableSafeMode) _settings.EnforceConstraints();
                if (_settings.GlobalSettings.ForceDefaultSettings) _settings.Restore();
            }

            _exclusiveMode = _settings.GlobalSettings.EnableExclusiveMode;
            _currentMode = (int)_settings.GlobalSettings.DefaultMode;
            _colors = _settings.ConsoleSettings.DefaultColors;
            Console.BackgroundColor = _settings.ConsoleSettings.BackgroundColor;

            if (_settings.RendererSettings.EnableThreadPriority)
                Thread.CurrentThread.Priority = _settings.RendererSettings.ThreadPriority;

            Config.Font.Font.SetCustomFont(Config.Font.FontFace.Consolas, 16, false); //always start at default type
            Config.Font.Font.SaveCurrentFont();

            if((_settings.FontSettings.EnableRasterFont || (_settings.GlobalSettings.EnableRasterOnDirectWrite && _settings.RendererSettings.RendererMode == RenderMode.DirectWrite)))
                Config.Font.Font.SetRasterFont(_settings.FontSettings.RasterFontType);

            if(_settings.FontSettings.EnableCustomFont)
            {
                Config.Font.Font.RestorePreviousFont();
                Config.Font.Font.SetCustomFont(
                    _settings.FontSettings.CustomFontFace,
                    _settings.FontSettings.CustomFontSize,
                    _settings.FontSettings.CustomFontBold,
                    _settings.FontSettings.CustomFontFaceOverride
                );
                Config.Font.Font.SaveCurrentFont();
            }


            ConsoleWindow.SetScreenSize(115, 35); //always launch at these defaults
            CLI.HandleCliArgs(args, _settings.GlobalSettings);

            HandleConsoleWindow();

            try
            {
                _visualizations = Utility.RefreshVisuals(_settings);

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
                    if (!_isPaused && _currentVisualization is IVolumeReactive visualization) visualization.Update(volume);
                };

                audioCapture.OnVolumeSpike += (volume) =>
                {
                    if (_isPaused) return;

                    if (_settings.ConsoleSettings.EnableFlashOnBeat)
                        ConsoleWindow.FlashWindowOnBeat(_settings.ConsoleSettings.FlashOnBeatCount);

                    if (_currentVisualization is ISpikeReactive visualization) visualization.OnSpike(volume);
                };

                audioCapture.OnFrequencyData += (bands) =>
                {
                    if (_currentVisualization is IFrequencyReactive visualization) visualization.OnFrequencyData(bands);
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
                                _sampleWindowStart = _stopWatch.ElapsedTicks;
                                _framesInWindow = 0;
                            }
                        }

                        _currentVisualization = _visualizations[_currentMode];
                        audioCapture.UpdateCurrentVisualization(_currentVisualization); //update in case FFT needed

                        //redraw
                        buffer.Clear();
                        _currentVisualization.Draw(buffer);

                        //debug bar
                        if(_settings.GlobalSettings.EnableDebugMode)
                        {
                            ConsoleColor debugTextColor = ConsoleColor.Gray;
                            ConsoleColor fpsColor = ConsoleColor.Gray;

                            if(Console.BackgroundColor == ConsoleColor.Black || Console.BackgroundColor == ConsoleColor.DarkGray)
                            {
                                debugTextColor = ConsoleColor.Gray;
                                fpsColor = ConsoleColor.Yellow;
                            }
                            else if(Console.BackgroundColor == ConsoleColor.White || Console.BackgroundColor == ConsoleColor.Gray)
                            {
                                debugTextColor = ConsoleColor.DarkGray;
                                fpsColor = ConsoleColor.DarkBlue;
                            }

                            if (_currentVisualization is Rings rings)
                            {
                                string ringsStatus = $"RE[V]ERSE:{(_settings.RingsSettings.ReverseMode ? "ON" : "OFF")} | [S]OLID:{(_settings.RingsSettings.SolidColor ? "ON" : "OFF")} | [C]OLOR:{Utility.FormatEnum(_settings.RingsSettings.ColorMode)} | RANDO[M] CHARS:{(_settings.RingsSettings.CharRandomizer ? "ON" : "OFF")} | [-/=] RADIUS:{_settings.RingsSettings.Radius} | [9/0] MAX RINGS:{_settings.RingsSettings.MaxRings} | [O/P] SEGMENTS:{_settings.RingsSettings.Segments}";
                                buffer.DrawString(0, buffer.Height - 3, ringsStatus, debugTextColor);
                                //data in top left
                                buffer.DrawString(0, 3, $"RINGS:{rings.RingCount}/{_settings.WaterfallSettings.MaxStreams}", debugTextColor);
                            }

                            if (_currentVisualization is Waterfall waterfall)
                            {
                                //controls
                                string waterfallStatus = $"[R]AINBOW:{(_settings.WaterfallSettings.RainbowMode ? "ON" : "OFF")} | [M]ODE:{Utility.FormatEnum(_settings.WaterfallSettings.Mode)} | RE[V]ERSE:{(_settings.WaterfallSettings.ReverseMode ? "ON" : "OFF")} | [-/=] THICKNESS: {_settings.WaterfallSettings.Thickness}";

                                if (!_settings.WaterfallSettings.RainbowMode)
                                    waterfallStatus += $" | [C]OLOR:{Utility.FormatEnum(_settings.WaterfallSettings.Color)}";

                                if (_settings.WaterfallSettings.Mode == WaterfallMode.Normal)
                                    waterfallStatus += $" | [O]RIGIN:{Utility.FormatEnum(_settings.WaterfallSettings.Origin)}";

                                buffer.DrawString(0, buffer.Height - 3, waterfallStatus, debugTextColor);

                                //data in top left
                                buffer.DrawString(0, 3, $"STREAMS:{waterfall.StreamCount}/{_settings.WaterfallSettings.MaxStreams}", debugTextColor);
                                
                            }

                            if(_currentVisualization is Shape)
                            {
                                string shapeStatus = $"[S]HAPE:{Utility.FormatEnum(_settings.ShapeSettings.Type)} | LA[Y]OUT:{Utility.FormatEnum(_settings.ShapeSettings.Layout)} | [C]OLOR:{Utility.FormatEnum(_settings.ShapeSettings.UniformColor)} | [F]ILL:{(_settings.ShapeSettings.FillMode ? "ON" : "OFF")} | RE[V]ERSE:{(_settings.ShapeSettings.ReverseMode ? "ON" : "OFF")} | SMOO[T]H:{(_settings.ShapeSettings.SmoothMode ? "ON" : "OFF")} | [-/=] SIZE:{_settings.ShapeSettings.MaxSizePercent:F2}";

                                if (_settings.ShapeSettings.Type == ShapeType.Polygon)
                                    shapeStatus += $" | [9/0] VERT:{_settings.ShapeSettings.PolygonSides}";
                                if(_settings.ShapeSettings.Layout != ShapeLayout.Single && _settings.ShapeSettings.Layout != ShapeLayout.Concentric)
                                    shapeStatus += $" | [O/P] COUNT:{_settings.ShapeSettings.Count}";
                                if(_settings.ShapeSettings.Layout == ShapeLayout.Concentric)
                                    shapeStatus += $" | [O/P] COUNT:{_settings.ShapeSettings.ConcentricLayers}";

                                buffer.DrawString(0, buffer.Height - 3, shapeStatus, debugTextColor);
                            }

                            if (_currentVisualization is Equalizer)
                            {
                                try
                                {
                                    int debugBufferHeight = 3; //start pos for printing eq data
                                    string[] frequencyData = audioCapture.FftAnalyzer.GetBandFrequencyData(_settings.FftSettings.BandCount);
                                    bool mirrorMode = _settings.EqualizerSettings.Direction == EqDirection.Mirror;

                                    for (int i = 0; i < (!mirrorMode ? frequencyData.Length : frequencyData.Length / 2); i++)
                                    {
                                        //in the top left
                                        buffer.DrawString(0, debugBufferHeight, frequencyData[i], fpsColor);
                                        debugBufferHeight++;
                                    }
                                }
                                catch(Exception ex)
                                {
                                    buffer.DrawString(0, 3, "NO FREQUENCY DATA", debugTextColor);
                                }
                            }

                            string modeName = Utility.GetModeName(_currentMode);
                            string line1 = $"VOL: {audioCapture.SmoothedVolume:F2} | PEAK: {audioCapture.PeakVolume:F2} | RMS: {audioCapture.RMS:F2}";
                            string line2 = $"MODE: {modeName}";
                            string line3 = $"LOCK: {(_settings.GlobalSettings.EnableControlLock ? "ON" : "OFF")}";

                            buffer.DrawString(0, 0, line1, fpsColor);
                            buffer.DrawString(0, 1, line2, fpsColor);
                            buffer.DrawString(0, 2, line3, fpsColor);

                            if (_settings.GlobalSettings.ShowGlobalControls)
                            {
                                string controls = "[TAB] MODE | [SPACE] PAUSE | [D]EBUG | [L]OCK | [F1] SAVE | [F2] LOAD | [F3] DEFAULTS | [F5] FULL | [ESC] EXIT";
                                buffer.DrawString(0, buffer.Height - 1, controls, debugTextColor);
                            }

                            //fps stuff
                            int rightX = buffer.Width - 10; //top right corner
                            buffer.DrawString(rightX, 0, $"FPS:{_currentFps,6:F1}", fpsColor);
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
                    //exit
                    case ConsoleKey.Escape:
                        if (_settings.GlobalSettings.SaveOnExit)
                            SettingsManager.Save(_settings);

                        audioCapture?.Stop();

                        if (_exclusiveMode)
                            ConsoleWindow.ExclusiveMode(false);

                        Environment.Exit(0);
                        break;

                    //pause
                    case ConsoleKey.Spacebar:
                        if (_settings.GlobalSettings.EnableControlLock) return;
                        _isPaused = !_isPaused;
                        break;

                    //change visual mode
                    case ConsoleKey.Tab:
                        if (_settings.GlobalSettings.EnableControlLock) return;
                        if(!_isPaused)
                            _currentMode = (_currentMode + 1) % _visualizations.Count;
                        break;

                    //toggle debug mode
                    case ConsoleKey.D:
                        if(!_isPaused)
                            _settings.GlobalSettings.EnableDebugMode = !_settings.GlobalSettings.EnableDebugMode;
                        break;

                    //lock controls
                    case ConsoleKey.L:
                        if (!_isPaused)
                            _settings.GlobalSettings.EnableControlLock = !_settings.GlobalSettings.EnableControlLock;
                        break;

                    //save
                    case ConsoleKey.F1:
                        {
                            if (_settings.GlobalSettings.EnableControlLock) return;
                            string normalConsoleTitle = Console.Title ?? "";
                            string saveStatusIndicator = "";
                            _isSavingOrLoading = true;
                            try
                            {
                                SettingsManager.Save(_settings);
                                saveStatusIndicator = normalConsoleTitle + " [ SAVED ]";
                            }
                            catch (Exception e)
                            {
                                saveStatusIndicator = normalConsoleTitle + " [ SAVE FAILED ]";
                            }

                            var messageEndTime = DateTime.UtcNow.AddMilliseconds(500);
                            while (_isSavingOrLoading)
                            {
                                Console.Title = saveStatusIndicator;
                                ConsoleWindow.FlashWindowOnBeat(5);

                                if (DateTime.UtcNow >= messageEndTime)
                                    _isSavingOrLoading = false;
                            }
                            Console.Title = normalConsoleTitle;
                            break;
                        }

                    //load
                    case ConsoleKey.F2:
                        {
                            if (_settings.GlobalSettings.EnableControlLock) return;
                            #pragma warning disable CA1416 // Validate platform compatibility
                            string normalConsoleTitle = Console.Title;
                               #pragma warning restore CA1416 // Validate platform compatibility
                            string saveStatusIndicator = "";
                            _isSavingOrLoading = true;
                            try
                            {
                                _settings = SettingsManager.Load();
                                audioCapture.UpdateSettings(_settings);
                                _visualizations = Utility.RefreshVisuals(_settings);
                                _currentVisualization = _visualizations[_currentMode];

                                saveStatusIndicator = normalConsoleTitle + " [ LOADED ]";
                            }
                            catch (Exception e)
                            {
                                saveStatusIndicator = normalConsoleTitle + " [ LOAD FAILED ]";
                            }

                            var messageEndTime = DateTime.UtcNow.AddMilliseconds(500);
                            while (_isSavingOrLoading)
                            {
                                Console.Title = saveStatusIndicator;
                                ConsoleWindow.FlashWindowOnBeat(5);

                                if (DateTime.UtcNow >= messageEndTime)
                                    _isSavingOrLoading = false;
                            }
                            Console.Title = normalConsoleTitle;
                            break;
                        }
                    //restore
                    case ConsoleKey.F3:
                        if (_settings.GlobalSettings.EnableControlLock) return;
                        _settings.Restore();
                        audioCapture.UpdateSettings(_settings);
                        _visualizations = Utility.RefreshVisuals(_settings);
                        _currentVisualization = _visualizations[_currentMode];
                        buffer.UpdateBackgroundColor(_settings.ConsoleSettings.BackgroundColor);
                        break;

                    //full screen
                    case ConsoleKey.F5:
                        _exclusiveMode = !_exclusiveMode;
                        ConsoleWindow.ExclusiveMode(_exclusiveMode);
                        
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
                            if(_settings.GlobalSettings.EnableRasterOnDirectWrite)
                            {
                                if (_settings.RendererSettings.RendererMode == RenderMode.DirectWrite)
                                    Config.Font.Font.SetRasterFont(_settings.FontSettings.RasterFontType);
                                else
                                    Config.Font.Font.RestorePreviousFont();
                            }
                            
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
                            _settings.ShapeSettings.ReverseMode = !_settings.ShapeSettings.ReverseMode;
                        break;

                    case ConsoleKey.C:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                        {
                            RingColorMode[] cycle = { RingColorMode.Light, RingColorMode.Red, RingColorMode.Green, RingColorMode.Blue, RingColorMode.Yellow, RingColorMode.RainbowLight, RingColorMode.RainbowDark, RingColorMode.Dark };
                            _settings.RingsSettings.ColorMode = Utility.CycleNext(cycle, _settings.RingsSettings.ColorMode);
                        }

                        if(_currentVisualization is Waterfall && !_settings.WaterfallSettings.RainbowMode)
                            _settings.WaterfallSettings.Color = Utility.CycleNext(_colors, _settings.WaterfallSettings.Color);


                        if(_currentVisualization is Shape)
                            _settings.ShapeSettings.UniformColor = Utility.CycleNext(_colors, _settings.ShapeSettings.UniformColor);
                        break;

                    case ConsoleKey.F:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Shape)
                            _settings.ShapeSettings.FillMode = !_settings.ShapeSettings.FillMode;
                        break;

                    case ConsoleKey.S:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if(_currentVisualization is Rings)
                            _settings.RingsSettings.SolidColor = !_settings.RingsSettings.SolidColor;

                        if(_currentVisualization is Shape)
                            _settings.ShapeSettings.Type = Utility.CycleNextEnum(_settings.ShapeSettings.Type);
                        break;

                    case ConsoleKey.Y:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Shape)
                            _settings.ShapeSettings.Layout = Utility.CycleNextEnum(_settings.ShapeSettings.Layout);
                        break;

                    case ConsoleKey.T:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Shape)
                            _settings.ShapeSettings.SmoothMode = !_settings.ShapeSettings.SmoothMode;
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
                            if (_settings.ShapeSettings.Layout == ShapeLayout.Single) return;

                            if (_settings.ShapeSettings.Layout == ShapeLayout.Concentric)
                            {
                                int layerCount = Math.Max(1, _settings.ShapeSettings.ConcentricLayers - 1);
                                _settings.ShapeSettings.ConcentricLayers = layerCount;
                                return;
                            }

                            int shapeCount = Math.Max(1, _settings.ShapeSettings.Count - 1);
                            _settings.ShapeSettings.Count = shapeCount;
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
                            if (_settings.ShapeSettings.Layout == ShapeLayout.Single) return;

                            if (_settings.ShapeSettings.Layout == ShapeLayout.Concentric)
                            {
                                int layerCount = Math.Min(10, _settings.ShapeSettings.ConcentricLayers + 1);
                                _settings.ShapeSettings.ConcentricLayers = layerCount;
                                return;
                            }
                            int shapeCount = Math.Min(4, _settings.ShapeSettings.Count + 1);
                            _settings.ShapeSettings.Count = shapeCount;
                        }
                        break;

                    case ConsoleKey.OemMinus:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                        {
                            _settings.RingsSettings.Radius = Math.Max(1, _settings.RingsSettings.Radius - 2);
                            _settings.RingsSettings.RadiusMax = Math.Max(_settings.RingsSettings.Radius + 2, _settings.RingsSettings.RadiusMax - 2);
                        }

                        if (_currentVisualization is Waterfall)
                            _settings.WaterfallSettings.Thickness = Math.Max(1, _settings.WaterfallSettings.Thickness - 1);

                        if (_currentVisualization is Shape)
                            _settings.ShapeSettings.MaxSizePercent = Math.Max(0.05f, _settings.ShapeSettings.MaxSizePercent - 0.02f);
                        break;

                    case ConsoleKey.OemPlus:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                        {
                            _settings.RingsSettings.Radius = Math.Min(195, _settings.RingsSettings.Radius + 2);
                            _settings.RingsSettings.RadiusMax = Math.Min(200, _settings.RingsSettings.RadiusMax + 2);
                            if (_settings.RingsSettings.RadiusMax <= _settings.RingsSettings.Radius)
                                _settings.RingsSettings.RadiusMax = _settings.RingsSettings.Radius + 2;
                        }

                        if (_currentVisualization is Waterfall)
                            _settings.WaterfallSettings.Thickness = Math.Min(10, _settings.WaterfallSettings.Thickness + 1);

                        if (_currentVisualization is Shape)
                            _settings.ShapeSettings.MaxSizePercent = Math.Min(1.0f, _settings.ShapeSettings.MaxSizePercent + 0.02f);
                        break;

                    case ConsoleKey.D9:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                            _settings.RingsSettings.MaxRings = Math.Max(3, _settings.RingsSettings.MaxRings - 1);

                        if (_currentVisualization is Shape)
                        {
                            if (_settings.ShapeSettings.Type != ShapeType.Polygon) return;

                            int[] validSides = { 5, 6, 8, 10, 12 };

                            _settings.ShapeSettings.PolygonSides = Utility.CyclePrevious(validSides, _settings.ShapeSettings.PolygonSides, true);
                        }
                        break;

                    case ConsoleKey.D0:
                        if (_isPaused || _settings.GlobalSettings.EnableControlLock) return;

                        if(_currentVisualization is Rings)
                            if (_currentVisualization is Rings)
                                _settings.RingsSettings.MaxRings = Math.Min(20, _settings.RingsSettings.MaxRings + 1);

                        if (_currentVisualization is Shape)
                        {
                            if (_settings.ShapeSettings.Type != ShapeType.Polygon) return;

                            int[] validSides = { 5, 6, 8, 10, 12 };

                            _settings.ShapeSettings.PolygonSides = Utility.CycleNext(validSides, _settings.ShapeSettings.PolygonSides, true);
                        }
                        break;
                }
            }
        }
        static void HandleConsoleWindow()
        {

            if (_settings.ConsoleSettings.DisableCursor)
                Console.CursorVisible = false;

            //manage window features
            if (_settings.ConsoleSettings.DisableTitleBar && !_exclusiveMode)
                ConsoleWindow.DisableTitleBar(); //TODO: still see a bit of border, likely DWM border

            if(_settings.ConsoleSettings.DisableScrollBars && !_exclusiveMode)
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
            if (_exclusiveMode)
                ConsoleWindow.ExclusiveMode(true);
            else if (_settings.ConsoleSettings.LaunchMaximized)
                ConsoleWindow.SetFullScreen();
            else if (_settings.ConsoleSettings.EnableCustomWindowSize)
                ConsoleWindow.SetScreenSize(_settings.ConsoleSettings.CustomWindowWidth, _settings.ConsoleSettings.CustomWindowHeight);

            //position
            if (!_settings.ConsoleSettings.LaunchMaximized && !_exclusiveMode)
            {
                if (_settings.ConsoleSettings.LaunchAt && _settings.ConsoleSettings.LaunchAtX >= 0 && _settings.ConsoleSettings.LaunchAtY >= 0)
                    ConsoleWindow.LaunchConsoleAt(_settings.ConsoleSettings.LaunchAtX, _settings.ConsoleSettings.LaunchAtY);
                else if (_settings.ConsoleSettings.LaunchInCenter)
                    ConsoleWindow.LaunchConsoleCenter();
            }

            if (_settings.ConsoleSettings.DisableWindowResize)
                ConsoleWindow.DisableResize();

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
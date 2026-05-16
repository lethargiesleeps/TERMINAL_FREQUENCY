#nullable disable warnings
using System;
using System.Diagnostics;
using System.Threading;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Core.Audio;
using TERMINAL_FREQUENCY.Core.CLI;
using TERMINAL_FREQUENCY.Core.Rendering;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Visualization.Cube;
using TERMINAL_FREQUENCY.Visualization.Equalizer;
using TERMINAL_FREQUENCY.Visualization.NoiseField;
using TERMINAL_FREQUENCY.Visualization.Rings;
using TERMINAL_FREQUENCY.Visualization.Shape;
using TERMINAL_FREQUENCY.Visualization.Waterfall;

namespace TERMINAL_FREQUENCY
{
    /// <summary>
    /// Main execution point of program. Launch setup is done here.
    /// Handles input, rendering loop, creation and parsing of settings, and debug mode information.
    /// </summary>
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
        private static int _selectedDeviceIndex = -1;

        //fps calculations
        private static Stopwatch _stopWatch;
        private static long _sampleWindowStart = 0;
        private static int _framesInWindow = 0;
        private static float _currentFps = 0;

        private const float SAMPLE_DURATION_SECONDS = 1.0f;

        /// <summary>
        /// Main function of program where everything happens.
        /// </summary>
        /// <param name="args">Command line arguments, not fully implemented</param>
        static void Main(string[] args)
        {
            //ensure settings properly configured
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
                if (_settings.Global.EnableSafeMode) _settings.EnforceConstraints();
                if (_settings.Global.ForceDefaultSettings) _settings.Restore();
            }

            //setup console settings, font, audio capture and renderer
            _exclusiveMode = _settings.Global.EnableExclusiveMode;
            _currentMode = (int)_settings.Global.DefaultMode;
            _colors = _settings.Window.DefaultColors;
            Console.BackgroundColor = _settings.Window.BackgroundColor;

            if (_settings.Renderer.EnableThreadPriority)
                Thread.CurrentThread.Priority = _settings.Renderer.ThreadPriority;

            Config.Font.Font.SetCustomFont(Config.Font.FontFace.Consolas, 16, false); //always start at default type
            Config.Font.Font.SaveCurrentFont();

            if((_settings.Font.EnableRasterFont || (_settings.Global.EnableRasterOnDirectWrite && _settings.Renderer.RendererMode == RenderMode.DirectWrite)))
                Config.Font.Font.SetRasterFont(_settings.Font.RasterFontType);

            if(_settings.Font.EnableCustomFont)
            {
                Config.Font.Font.RestorePreviousFont();
                Config.Font.Font.SetCustomFont(
                    _settings.Font.CustomFontFace,
                    _settings.Font.CustomFontSize,
                    _settings.Font.CustomFontBold,
                    _settings.Font.CustomFontFaceOverride
                );
                Config.Font.Font.SaveCurrentFont();
            }


            ConsoleWindow.SetScreenSize(115, 35); //always launch at these defaults
            CLI.HandleCliArgs(args, _settings.Global);

            HandleConsoleWindow(); //Sets all windows settings

            //if enabled, user selects audio device, otherwise use loopback capture
            if (_settings.AudioCapture.UserSelectedDevice && _settings.AudioCapture.SpecifyAudioDevice)
                _selectedDeviceIndex = Utility.SelectAudioDevice();

            try
            {
                _visualizations = Utility.RefreshVisuals(_settings); //instantiate new visual classes

                //configure audio capture
                AudioCapture? audioCapture = _settings.AudioCapture.SpecifyAudioDevice 
                    ? new AudioCapture(_settings, (_selectedDeviceIndex > -1 ? _selectedDeviceIndex : _settings.AudioCapture.AudioDeviceIndex)) 
                    : new AudioCapture(_settings);

                if (audioCapture == null)
                {
                    Console.WriteLine("\nNo audio device selected. Exiting...");
                    Console.ReadKey();
                    return;
                }

                //configure renderer
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

                    if (_settings.Window.EnableFlashOnBeat)
                        ConsoleWindow.FlashWindowOnBeat(_settings.Window.FlashOnBeatCount);

                    if (_currentVisualization is ISpikeReactive visualization) visualization.OnSpike(volume);
                };

                audioCapture.OnFrequencyData += (bands) =>
                {
                    if (_currentVisualization is IFrequencyReactive visualization) visualization.OnFrequencyData(bands);
                };


                //start capture
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
                        if(_settings.Global.EnableDebugMode)
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
                        if(_settings.Global.EnableDebugMode)
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
                                string ringsStatus = $"RE[V]ERSE:{(_settings.Rings.ReverseMode ? "ON" : "OFF")} | [S]OLID:{(_settings.Rings.SolidColor ? "ON" : "OFF")} | [C]OLOR:{Utility.FormatEnum(_settings.Rings.ColorMode)} | RANDO[M] CHARS:{(_settings.Rings.CharRandomizer ? "ON" : "OFF")} | [-/=] RADIUS:{_settings.Rings.Radius} | [9/0] MAX RINGS:{_settings.Rings.MaxRings} | [O/P] SEGMENTS:{_settings.Rings.Segments}";
                                buffer.DrawString(0, buffer.Height - 3, ringsStatus, debugTextColor);
                                //data in top left
                                buffer.DrawString(0, 3, $"RINGS:{rings.RingCount}/{_settings.Waterfall.MaxStreams}", debugTextColor);
                            }

                            if (_currentVisualization is Waterfall waterfall)
                            {
                                //controls
                                string waterfallStatus = $"[R]AINBOW:{(_settings.Waterfall.RainbowMode ? "ON" : "OFF")} | [M]ODE:{Utility.FormatEnum(_settings.Waterfall.Mode)} | RE[V]ERSE:{(_settings.Waterfall.ReverseMode ? "ON" : "OFF")} | [-/=] THICKNESS: {_settings.Waterfall.Thickness}";

                                if (!_settings.Waterfall.RainbowMode)
                                    waterfallStatus += $" | [C]OLOR:{Utility.FormatEnum(_settings.Waterfall.Color)}";

                                if (_settings.Waterfall.Mode == WaterfallMode.Normal)
                                    waterfallStatus += $" | [O]RIGIN:{Utility.FormatEnum(_settings.Waterfall.Origin)}";

                                buffer.DrawString(0, buffer.Height - 3, waterfallStatus, debugTextColor);

                                //data in top left
                                buffer.DrawString(0, 3, $"STREAMS:{waterfall.StreamCount}/{_settings.Waterfall.MaxStreams}", debugTextColor);
                                
                            }

                            if(_currentVisualization is Shape)
                            {
                                string shapeStatus = $"[S]HAPE:{Utility.FormatEnum(_settings.Shape.Type)} | LA[Y]OUT:{Utility.FormatEnum(_settings.Shape.Layout)} | [C]OLOR:{Utility.FormatEnum(_settings.Shape.UniformColor)} | [F]ILL:{(_settings.Shape.FillMode ? "ON" : "OFF")} | RE[V]ERSE:{(_settings.Shape.ReverseMode ? "ON" : "OFF")} | SMOO[T]H:{(_settings.Shape.SmoothMode ? "ON" : "OFF")} | [-/=] SIZE:{_settings.Shape.MaxSizePercent:F2}";

                                if (_settings.Shape.Type == ShapeType.Polygon)
                                    shapeStatus += $" | [9/0] VERT:{_settings.Shape.PolygonSides}";
                                if(_settings.Shape.Layout != ShapeLayout.Single && _settings.Shape.Layout != ShapeLayout.Concentric)
                                    shapeStatus += $" | [O/P] COUNT:{_settings.Shape.Count}";
                                if(_settings.Shape.Layout == ShapeLayout.Concentric)
                                    shapeStatus += $" | [O/P] COUNT:{_settings.Shape.ConcentricLayers}";

                                buffer.DrawString(0, buffer.Height - 3, shapeStatus, debugTextColor);
                            }

                            if(_currentVisualization is NoiseField)
                            {
                                // - +, O P, 9 0, 7 8
                                string fieldStatus = $"[-/+] THRESHOLD:{_settings.NoiseField.VolumeThreshold:F2} | [O/P] SENS:{_settings.NoiseField.Sensitivity:F2} | [9/0] JITTER:{_settings.NoiseField.JitterAmount:F2} | [7/8] SPREAD:{_settings.NoiseField.SpreadRadius:F2}";
                                string fieldStatus2 = $"[C]OLOR:{Utility.FormatEnum(_settings.NoiseField.Color)} | CEN[T]ER:{(_settings.NoiseField.CenterOrigin ? "ON" : "OFF")} | DUAL CHAR[S]ETS:{(_settings.NoiseField.UseDualCharacterSets ? "ON" : "OFF")} | CLR PATTE[R]N:{(_settings.NoiseField.UseColorPattern ? "ON" : "OFF")}";
                                buffer.DrawString(0, buffer.Height - 3, fieldStatus, debugTextColor);
                                buffer.DrawString(0, buffer.Height - 2, fieldStatus2, debugTextColor);
                            }

                            if(_currentVisualization is Cube)
                            {
                                float globalSpeed = (_settings.Cube.RotationSpeedY + _settings.Cube.RotationSpeedX + _settings.Cube.RotationSpeedZ) / 3;
                                string cubeStatus1 = $"[M]ODE:{Utility.FormatEnum(_settings.Cube.RotationMode)} | [R]OTATION:{Utility.FormatEnum(_settings.Cube.Direction)} | [O/P] GLOBAL SPEED:{globalSpeed:F3} | [9/0] SIZE:{_settings.Cube.ZoomLevel:F2}";
                                string cubeStatus2 = $"[C]OLOR:{Utility.FormatEnum(_settings.Cube.Color)} | FREEZE [X]:{(_settings.Cube.FreezeXRotation ? "ON" : "OFF")} | FREEZE [Y]:{(_settings.Cube.FreezeYRotation ? "ON" : "OFF")} | FREEZE [Z]:{(_settings.Cube.FreezeZRotation ? "ON" : "OFF")} | PUL[S]E:{(_settings.Cube.PulseEnabled ? "ON" : "OFF")}";

                                if (_settings.Cube.PulseEnabled)
                                    cubeStatus2 += $" | [7/8] INTENSITY:{_settings.Cube.PulseIntensity:F3}";

                                buffer.DrawString(0, buffer.Height - 3, cubeStatus1, debugTextColor);
                                buffer.DrawString(0, buffer.Height - 2, cubeStatus2, debugTextColor);

                            }
                            if (_currentVisualization is IFrequencyReactive)
                            {
                                bool skipFrequencyData = _currentVisualization is Cube cube && _settings.Cube.RotationMode != CubeRotationMode.OnFrequency;

                                //draw frequency data
                                try
                                {
                                    if(!skipFrequencyData)
                                    {
                                        int debugBufferHeight = 4;
                                        int debugBufferWidth = 0;
                                        string[] frequencyData = audioCapture.FftAnalyzer.GetBandFrequencyData(_settings.Fft.BandCount);
                                        int bandsPerColumn = frequencyData.Length > 16 ? 8 : 4;

                                        for (int i = 0; i < frequencyData.Length; i++)
                                        {
                                            if (i > 0 && i % bandsPerColumn == 0)
                                            {
                                                debugBufferWidth += 35; //shift to the right
                                                debugBufferHeight = 4;
                                            }


                                            buffer.DrawString(debugBufferWidth, debugBufferHeight, frequencyData[i], fpsColor);
                                            debugBufferHeight++;
                                        }
                                    }
                                }
                                catch(Exception ex)
                                {
                                    if(!skipFrequencyData)
                                        buffer.DrawString(0, 4, "NO FREQUENCY DATA", debugTextColor);
                                }


                                //global frequency controls
                                var controls = new List<string>
                                {
                                    $"[-/+] BANDS:{_settings.Fft.BandCount}",
                                    $"[9/0] SENSITIVITY:{_settings.Fft.Sensitivity:F1}",
                                };

                                if (_currentVisualization is Cube) controls.RemoveAt(1);

                                //equalizer specific
                                if (_currentVisualization is Equalizer)
                                {
                                    controls.Add($"[C]OLOR MODE:{_settings.Equalizer.ColorMode.ToString().ToUpper()}");
                                    controls.Add($"DIREC[T]ION:{_settings.Equalizer.Direction.ToString().ToUpper()}");
                                    controls.Add($"[S]OLID:{(_settings.Equalizer.SolidBands ? "ON" : "OFF")}");
                                    controls.Add($"[O]RIGIN: {_settings.Equalizer.Origin.ToString().ToUpper()}");

                                    if(_settings.Equalizer.Origin == VisualizationOrigin.Center)
                                        controls.Add($"HO[R]IZONTAL: {(_settings.Equalizer.HorizontalWhenCentered ? "ON" : "OFF")}");
                                }

                                //draw controls, below FPS
                                if(!skipFrequencyData)
                                {
                                    int startY = 2;
                                    for (int i = 0; i < controls.Count; i++)
                                    {
                                        buffer.DrawString(buffer.Width - 28, startY + i, controls[i], fpsColor);
                                    }
                                }

                            }

                            string modeName = Utility.GetModeName(_currentMode);
                            string line1 = $"VOL: {audioCapture.SmoothedVolume:F2} | PEAK: {audioCapture.PeakVolume:F2} | RMS: {audioCapture.RMS:F2}";
                            string line2 = $"MODE: {modeName}";
                            string line3 = $"LOCK: {(_settings.Global.EnableControlLock ? "ON" : "OFF")} | DEVICE: {audioCapture.GetDeviceName()}";

                            buffer.DrawString(0, 0, line2, debugTextColor);
                            buffer.DrawString(0, 1, line3, debugTextColor);
                            buffer.DrawString(0, 2, line1, ConsoleColor.Green);


                            if (_settings.Global.ShowGlobalControls)
                            {
                                string controls = "[TAB] MODE | [SPACE] PAUSE | [D]EBUG | [L]OCK | [F1] SAVE | [F2] LOAD | [F3] DEFAULTS | [F5] FULL | [ESC] EXIT";
                                buffer.DrawString(0, buffer.Height - 1, controls, debugTextColor);
                            }

                            //fps stuff
                            int rightX = buffer.Width - 10; //top right corner
                            buffer.DrawString(rightX, 0, $"FPS:{_currentFps,6:F1}", fpsColor);
                        }

                        buffer.Render(); //main render

                        //yield settings
                        long targetTicks = Stopwatch.Frequency / _settings.Renderer.TargetFps;

                        if(_settings.Renderer.EnableYield)
                            Thread.Sleep(_settings.Renderer.YieldTimeout);
                        else if(_settings.Renderer.EnableSpinWait)
                        {
                            while (_stopWatch.ElapsedTicks - frameStart < targetTicks)
                                Thread.SpinWait(_settings.Renderer.SpinWaitIterations);
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

        /// <summary>
        /// Checks for a keyboard press while in main loop and handles accordingly.
        /// </summary>
        /// <param name="audioCapture">Global AudioCapture instance.</param>
        /// <param name="buffer">Global ScreenBuffer (Renderer) instance</param>
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
                        if (_settings.Global.SaveOnExit)
                            SettingsManager.Save(_settings);

                        audioCapture?.Stop();

                        if (_exclusiveMode)
                            ConsoleWindow.ExclusiveMode(false);

                        Environment.Exit(0);
                        break;

                    //pause
                    case ConsoleKey.Spacebar:
                        if (_settings.Global.EnableControlLock) return;
                        _isPaused = !_isPaused;
                        break;

                    //change visual mode
                    case ConsoleKey.Tab:
                        if (_settings.Global.EnableControlLock) return;
                        if(!_isPaused)
                            _currentMode = (_currentMode + 1) % _visualizations.Count;
                        break;

                    //toggle debug mode
                    case ConsoleKey.D:
                        if(!_isPaused)
                            _settings.Global.EnableDebugMode = !_settings.Global.EnableDebugMode;
                        break;

                    //lock controls
                    case ConsoleKey.L:
                        if (!_isPaused)
                            _settings.Global.EnableControlLock = !_settings.Global.EnableControlLock;
                        break;

                    //save
                    case ConsoleKey.F1:
                        {
                            if (_settings.Global.EnableControlLock) return;
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
                            if (_settings.Global.EnableControlLock) return;
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
                        if (_settings.Global.EnableControlLock) return;
                        _settings.Restore();
                        audioCapture.UpdateSettings(_settings);
                        _visualizations = Utility.RefreshVisuals(_settings);
                        _currentVisualization = _visualizations[_currentMode];
                        buffer.UpdateBackgroundColor(_settings.Window.BackgroundColor);
                        break;

                    //full screen
                    case ConsoleKey.F5:
                        _exclusiveMode = !_exclusiveMode;
                        ConsoleWindow.ExclusiveMode(_exclusiveMode);
                        
                        break;
                    #endregion

                    case ConsoleKey.R:
                        if(_isPaused || _settings.Global.EnableControlLock) return;

                        if(_currentVisualization is Waterfall)
                            _settings.Waterfall.RainbowMode = !_settings.Waterfall.RainbowMode;

                        if (_currentVisualization is Equalizer)
                            if (_settings.Equalizer.Origin == VisualizationOrigin.Center)
                                _settings.Equalizer.HorizontalWhenCentered = !_settings.Equalizer.HorizontalWhenCentered;

                        if (_currentVisualization is Cube)
                            _settings.Cube.Direction = Utility.CycleNextEnum(_settings.Cube.Direction);
                        if(_currentVisualization is NoiseField)
                            _settings.NoiseField.UseColorPattern = !_settings.NoiseField.UseColorPattern;
                        break;

                    case ConsoleKey.M:
                        if(_isPaused)
                        {
                            buffer.CycleRenderMode();
                            if(_settings.Global.EnableRasterOnDirectWrite)
                            {
                                if (_settings.Renderer.RendererMode == RenderMode.DirectWrite)
                                    Config.Font.Font.SetRasterFont(_settings.Font.RasterFontType);
                                else
                                    Config.Font.Font.RestorePreviousFont();
                            }
                            
                            return;
                        }

                        if (_settings.Global.EnableControlLock) return;

                        if(_currentVisualization is Rings)
                            _settings.Rings.CharRandomizer = !_settings.Rings.CharRandomizer;

                        if(_currentVisualization is Waterfall)
                            _settings.Waterfall.Mode = Utility.CycleNextEnum(_settings.Waterfall.Mode);

                        if (_currentVisualization is Cube)
                            _settings.Cube.RotationMode = Utility.CycleNextEnum(_settings.Cube.RotationMode);
                        
                        break;

                    case ConsoleKey.V:
                        if(_isPaused || _settings.Global.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                            _settings.Rings.ReverseMode = !_settings.Rings.ReverseMode;

                        if (_currentVisualization is Waterfall)
                            _settings.Waterfall.ReverseMode = !_settings.Waterfall.ReverseMode;

                        if (_currentVisualization is Shape)
                            _settings.Shape.ReverseMode = !_settings.Shape.ReverseMode;
                        break;

                    case ConsoleKey.C:
                        if (_isPaused || _settings.Global.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                        {
                            RingColorMode[] cycle = { RingColorMode.Light, RingColorMode.Red, RingColorMode.Green, RingColorMode.Blue, RingColorMode.Yellow, RingColorMode.RainbowLight, RingColorMode.RainbowDark, RingColorMode.Dark };
                            _settings.Rings.ColorMode = Utility.CycleNext(cycle, _settings.Rings.ColorMode);
                        }

                        if(_currentVisualization is Waterfall && !_settings.Waterfall.RainbowMode)
                            _settings.Waterfall.Color = Utility.CycleNext(_colors, _settings.Waterfall.Color);


                        if(_currentVisualization is Shape)
                            _settings.Shape.UniformColor = Utility.CycleNext(_colors, _settings.Shape.UniformColor);

                        if (_currentVisualization is Equalizer)
                            _settings.Equalizer.ColorMode = Utility.CycleNextEnum(_settings.Equalizer.ColorMode);

                        if (_currentVisualization is Cube)
                            _settings.Cube.Color = Utility.CycleNext(_colors, _settings.Cube.Color);
                        
                        if(_currentVisualization is NoiseField)
                            _settings.NoiseField.Color = Utility.CycleNext(_colors, _settings.NoiseField.Color);

                        break;

                    case ConsoleKey.F:
                        if (_isPaused || _settings.Global.EnableControlLock) return;

                        if (_currentVisualization is Shape)
                            _settings.Shape.FillMode = !_settings.Shape.FillMode;
                        break;

                    case ConsoleKey.S:
                        if (_isPaused || _settings.Global.EnableControlLock) return;

                        if(_currentVisualization is Rings)
                            _settings.Rings.SolidColor = !_settings.Rings.SolidColor;

                        if(_currentVisualization is Shape)
                            _settings.Shape.Type = Utility.CycleNextEnum(_settings.Shape.Type);

                        if (_currentVisualization is Equalizer)
                            _settings.Equalizer.SolidBands = !_settings.Equalizer.SolidBands;

                        if(_currentVisualization is Cube)
                            _settings.Cube.PulseEnabled = !_settings.Cube.PulseEnabled;

                        if(_currentVisualization is NoiseField)
                            _settings.NoiseField.UseDualCharacterSets = !_settings.NoiseField.UseDualCharacterSets;
                        break;

                    case ConsoleKey.Y:
                        if (_isPaused || _settings.Global.EnableControlLock) return;

                        if (_currentVisualization is Shape)
                            _settings.Shape.Layout = Utility.CycleNextEnum(_settings.Shape.Layout);
                        
                        if(_currentVisualization is Cube)
                            _settings.Cube.FreezeYRotation = !_settings.Cube.FreezeYRotation;
                        break;

                    case ConsoleKey.X:
                        if (_currentVisualization is Cube)
                            _settings.Cube.FreezeXRotation = !_settings.Cube.FreezeXRotation;
                        break;

                    case ConsoleKey.Z:
                        if (_currentVisualization is Cube)
                            _settings.Cube.FreezeZRotation = !_settings.Cube.FreezeZRotation;
                        break;
                    
                    case ConsoleKey.T:
                        if (_isPaused || _settings.Global.EnableControlLock) return;

                        if (_currentVisualization is Shape)
                            _settings.Shape.SmoothMode = !_settings.Shape.SmoothMode;

                        if(_currentVisualization is Equalizer)
                            _settings.Equalizer.Direction = Utility.CycleNextEnum(_settings.Equalizer.Direction);

                        if(_currentVisualization is NoiseField)
                            _settings.NoiseField.CenterOrigin = !_settings.NoiseField.CenterOrigin;
                        break;

                    case ConsoleKey.O: //decrement 1 or Origin toggle
                        if (_isPaused || _settings.Global.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                        {
                            _settings.Rings.Segments = Math.Max(8, _settings.Rings.Segments - 2);
                            _settings.Rings.AmbientSegments = Math.Max(8, _settings.Rings.AmbientSegments - 2);
                        }

                        if (_currentVisualization is Waterfall && _settings.Waterfall.Mode == WaterfallMode.Normal)
                        {
                            VisualizationOrigin[] cycle = { VisualizationOrigin.Top, VisualizationOrigin.Right, VisualizationOrigin.Bottom, VisualizationOrigin.Left };
                            _settings.Waterfall.Origin = Utility.CycleNext(cycle, _settings.Waterfall.Origin);
                        }

                        if (_currentVisualization is Shape)
                        {
                            if (_settings.Shape.Layout == ShapeLayout.Single) return;

                            if (_settings.Shape.Layout == ShapeLayout.Concentric)
                            {
                                int layerCount = Math.Max(1, _settings.Shape.ConcentricLayers - 1);
                                _settings.Shape.ConcentricLayers = layerCount;
                                return;
                            }

                            int shapeCount = Math.Max(1, _settings.Shape.Count - 1);
                            _settings.Shape.Count = shapeCount;
                        }

                        if(_currentVisualization is Equalizer)
                            _settings.Equalizer.Origin = Utility.CycleNextEnum(_settings.Equalizer.Origin);
                        
                        if(_currentVisualization is Cube)
                        {
                            _settings.Cube.RotationSpeedX = Math.Max(0.002f, _settings.Cube.RotationSpeedX - 0.005f);
                            _settings.Cube.RotationSpeedY = Math.Max(0.002f, _settings.Cube.RotationSpeedY - 0.005f);
                            _settings.Cube.RotationSpeedX = Math.Max(0.001f, _settings.Cube.RotationSpeedZ - 0.005f);
                        }

                        if(_currentVisualization is NoiseField)
                            _settings.NoiseField.Sensitivity = Math.Max(0.25f, _settings.NoiseField.Sensitivity - 0.25f);
                        
                        break;

                    case ConsoleKey.P: //increment 1
                        if (_isPaused || _settings.Global.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                        {
                            _settings.Rings.Segments = Math.Min(100, _settings.Rings.Segments + 2);
                            _settings.Rings.AmbientSegments = Math.Min(80, _settings.Rings.AmbientSegments + 2);
                        }

                        if (_currentVisualization is Shape)
                        {
                            if (_settings.Shape.Layout == ShapeLayout.Single) return;

                            if (_settings.Shape.Layout == ShapeLayout.Concentric)
                            {
                                int layerCount = Math.Min(10, _settings.Shape.ConcentricLayers + 1);
                                _settings.Shape.ConcentricLayers = layerCount;
                                return;
                            }
                            int shapeCount = Math.Min(4, _settings.Shape.Count + 1);
                            _settings.Shape.Count = shapeCount;
                        }
                        

                        if (_currentVisualization is Cube)
                        {
                            _settings.Cube.RotationSpeedX = Math.Min(0.5f, _settings.Cube.RotationSpeedX + 0.005f);
                            _settings.Cube.RotationSpeedY = Math.Min(0.5f, _settings.Cube.RotationSpeedY + 0.005f);
                            _settings.Cube.RotationSpeedX = Math.Min(0.3f, _settings.Cube.RotationSpeedZ + 0.005f);
                        }


                        if (_currentVisualization is NoiseField)
                            _settings.NoiseField.Sensitivity = Math.Min(10f, _settings.NoiseField.Sensitivity + 0.25f);
                        
                        break;
                    
                    case ConsoleKey.OemMinus: //decrement 2
                        if (_isPaused || _settings.Global.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                        {
                            _settings.Rings.Radius = Math.Max(1, _settings.Rings.Radius - 2);
                            _settings.Rings.RadiusMax = Math.Max(_settings.Rings.Radius + 2, _settings.Rings.RadiusMax - 2);
                        }

                        if (_currentVisualization is Waterfall)
                            _settings.Waterfall.Thickness = Math.Max(1, _settings.Waterfall.Thickness - 1);

                        if (_currentVisualization is Shape)
                            _settings.Shape.MaxSizePercent = Math.Max(0.05f, _settings.Shape.MaxSizePercent - 0.02f);

                        if (_currentVisualization is IFrequencyReactive)
                            _settings.Fft.BandCount = Math.Max(4, _settings.Fft.BandCount - 2);

                        if (_currentVisualization is NoiseField)
                            _settings.NoiseField.VolumeThreshold = Math.Max(0.02f, _settings.NoiseField.VolumeThreshold - 0.02f);
                        break;

                    case ConsoleKey.OemPlus: //increment 2
                        if (_isPaused || _settings.Global.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                        {
                            _settings.Rings.Radius = Math.Min(195, _settings.Rings.Radius + 2);
                            _settings.Rings.RadiusMax = Math.Min(200, _settings.Rings.RadiusMax + 2);
                            if (_settings.Rings.RadiusMax <= _settings.Rings.Radius)
                                _settings.Rings.RadiusMax = _settings.Rings.Radius + 2;
                        }

                        if (_currentVisualization is Waterfall)
                            _settings.Waterfall.Thickness = Math.Min(10, _settings.Waterfall.Thickness + 1);

                        if (_currentVisualization is Shape)
                            _settings.Shape.MaxSizePercent = Math.Min(1.0f, _settings.Shape.MaxSizePercent + 0.02f);

                        if (_currentVisualization is IFrequencyReactive)
                            _settings.Fft.BandCount = Math.Min(32, _settings.Fft.BandCount  + 2);

                        if (_currentVisualization is NoiseField)
                            _settings.NoiseField.VolumeThreshold = Math.Min(1f, _settings.NoiseField.VolumeThreshold + 0.02f);
                        break;

                    case ConsoleKey.D7:
                        if (_isPaused || _settings.Global.EnableControlLock) return;
                        if(_currentVisualization is Cube)
                            _settings.Cube.PulseIntensity = Math.Max(0.05f, _settings.Cube.PulseIntensity - 0.025f);

                        if (_currentVisualization is NoiseField)
                            _settings.NoiseField.SpreadRadius = Math.Max(0.02f, _settings.NoiseField.SpreadRadius - 0.02f);
                        break;

                    case ConsoleKey.D8:
                        if (_isPaused || _settings.Global.EnableControlLock) return;
                        if (_currentVisualization is Cube)
                            _settings.Cube.PulseIntensity = Math.Min(1.5f, _settings.Cube.PulseIntensity + 0.025f);

                        if (_currentVisualization is NoiseField)
                            _settings.NoiseField.SpreadRadius = Math.Min(1f, _settings.NoiseField.SpreadRadius + 0.02f);
                        break;
                    
                    case ConsoleKey.D9: //decrement 3
                        if (_isPaused || _settings.Global.EnableControlLock) return;

                        if (_currentVisualization is Rings)
                            _settings.Rings.MaxRings = Math.Max(3, _settings.Rings.MaxRings - 1);

                        if (_currentVisualization is Shape)
                        {
                            if (_settings.Shape.Type != ShapeType.Polygon) return;

                            int[] validSides = { 5, 6, 8, 10, 12 };

                            _settings.Shape.PolygonSides = Utility.CyclePrevious(validSides, _settings.Shape.PolygonSides, true);
                        }

                        if (_currentVisualization is IFrequencyReactive)
                        {
                            if(_currentVisualization is Cube)
                                _settings.Cube.ZoomLevel = (float)Math.Max(5.0f, _settings.Cube.ZoomLevel - 0.5f);
                            else
                                _settings.Fft.Sensitivity = Math.Max(0.5f, _settings.Fft.Sensitivity - 0.05f);
                        }

                        if (_currentVisualization is NoiseField)
                            _settings.NoiseField.JitterAmount = Math.Max(0.02f, _settings.NoiseField.JitterAmount - 0.02f);
                        break;

                    case ConsoleKey.D0: //increment 3
                        if (_isPaused || _settings.Global.EnableControlLock) return;

                        if(_currentVisualization is Rings)
                            if (_currentVisualization is Rings)
                                _settings.Rings.MaxRings = Math.Min(20, _settings.Rings.MaxRings + 1);

                        if (_currentVisualization is Shape)
                        {
                            if (_settings.Shape.Type != ShapeType.Polygon) return;

                            int[] validSides = { 5, 6, 8, 10, 12 };

                            _settings.Shape.PolygonSides = Utility.CycleNext(validSides, _settings.Shape.PolygonSides, true);
                        }

                        if (_currentVisualization is IFrequencyReactive)
                        {
                            if (_currentVisualization is Cube)
                                _settings.Cube.ZoomLevel = (float)Math.Min(50.0f, _settings.Cube.ZoomLevel + 0.5f);
                            else
                                _settings.Fft.Sensitivity = Math.Min(3.0f, _settings.Fft.Sensitivity + 0.05f);

                        }

                        if (_currentVisualization is NoiseField)
                            _settings.NoiseField.JitterAmount = Math.Min(1f, _settings.NoiseField.JitterAmount + 0.02f);
                        break;

                    #region ChangeVisuals
                    case ConsoleKey.NumPad0:
                        if (_settings.Global.EnableControlLock) return;
                        _currentMode = 0;
                        break;

                    case ConsoleKey.NumPad1:
                        if (_settings.Global.EnableControlLock) return;
                        _currentMode = 1;
                        break;

                    case ConsoleKey.NumPad2:
                        if (_settings.Global.EnableControlLock) return;
                        _currentMode = 2;
                        break;

                    case ConsoleKey.NumPad3:
                        if (_settings.Global.EnableControlLock) return;
                        _currentMode = 3;
                        break;

                    case ConsoleKey.NumPad4:
                        if (_settings.Global.EnableControlLock) return;
                        _currentMode = 4;
                        break;

                    case ConsoleKey.NumPad5:
                        if (_settings.Global.EnableControlLock) return;
                        _currentMode = 5;
                        break;

                    case ConsoleKey.NumPad6:
                        if (_settings.Global.EnableControlLock) return;
                        _currentMode = 6;
                        break;
                        #endregion
                }
            }
        }

        /// <summary>
        /// Sets up the console window and configures based on values set in settings. Uses ConsoleWindow class extensively.
        /// </summary>
        /// <see cref="ConsoleWindow"/>
        static void HandleConsoleWindow()
        {

            if (_settings.Window.DisableCursor)
                Console.CursorVisible = false;

            //manage window features
            if (_settings.Window.DisableTitleBar && !_exclusiveMode)
                ConsoleWindow.DisableTitleBar(); //TODO: still see a bit of border, likely DWM border

            if(_settings.Window.DisableScrollBars && !_exclusiveMode)
                ConsoleWindow.DisableScrollBars();



            ConsoleWindow.SetAlwaysOnTop(_settings.Window.AlwaysOnTop);
            ConsoleWindow.SetOpacity((byte)_settings.Window.WindowOpacity);
            ConsoleWindow.SetClickThrough(_settings.Window.EnableClickThrough);

            if (_settings.Window.EnableWindowVibrancy)
                ConsoleWindow.SetWindowVibrancy(
                    (byte)_settings.Window.WindowVibrancyR,
                    (byte)_settings.Window.WindowVibrancyG,
                    (byte)_settings.Window.WindowVibrancyB,
                    (byte)_settings.Window.WindowVibrancyA
                );
            else if (_settings.Window.EnableWindowBlur)
                ConsoleWindow.SetWindowBlur(_settings.Window.EnableWindowBlur);

            //size
            if (_exclusiveMode)
                ConsoleWindow.ExclusiveMode(true);
            else if (_settings.Window.LaunchMaximized)
                ConsoleWindow.SetFullScreen();
            else if (_settings.Window.EnableCustomWindowSize)
                ConsoleWindow.SetScreenSize(_settings.Window.CustomWindowWidth, _settings.Window.CustomWindowHeight);

            //position
            if (!_settings.Window.LaunchMaximized && !_exclusiveMode)
            {
                if (_settings.Window.LaunchAt && _settings.Window.LaunchAtX >= 0 && _settings.Window.LaunchAtY >= 0)
                    ConsoleWindow.LaunchConsoleAt(_settings.Window.LaunchAtX, _settings.Window.LaunchAtY);
                else if (_settings.Window.LaunchInCenter)
                    ConsoleWindow.LaunchConsoleCenter();
            }

            if (_settings.Window.DisableWindowResize)
                ConsoleWindow.DisableResize();

            //manage process title
            if (_settings.Window.DisableAppTitle)
                Console.Title = string.Empty;
            else if (!string.IsNullOrEmpty(_settings.Window.CustomTitle))
                Console.Title = _settings.Window.CustomTitle;
            else
                Console.Title = _isChild ? $"TERMINAL FREQUENCY - Child" : "TERMINAL FREQUENCY";

            if (!_settings.Global.BypassStartupScreen)
                Utility.PrintStartup();
        }
    }
}
#pragma warning restore CS8618
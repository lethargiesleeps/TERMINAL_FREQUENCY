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
        static void Main(string[] args)
        {
            Console.Title = "TERMINAL FREQUENCY";
            Console.CursorVisible = false;

            Utility.PrintStartup();

            try
            {
                _visualizations = new List<IVisualization>()
                {
                    new KickCircle()
                };

                AudioCapture audioCapture = Config.Config.SPECIFY_AUDIO_DEVICE ? Utility.SelectAudioDevice() : new AudioCapture();

                if (audioCapture == null)
                {
                    Console.WriteLine("\nNo audio device selected. Exiting...");
                    Console.ReadKey();
                    return;
                }


                ScreenBuffer buffer = new ScreenBuffer();
                IVisualization currentVisualization = _visualizations[_currentMode];

                //register audio events
                audioCapture.OnVolumeUpdated += (volume) =>
                {
                    if (!_isPaused) currentVisualization.Update(volume);
                };

                audioCapture.OnVolumeSpike += (volume) =>
                {
                    if (_isPaused) return;

                    if (currentVisualization is KickCircle kickCircle)
                        kickCircle.OnSpike();
                };

                //capture the audio
                audioCapture.Start();

                //render
                while (true)
                {
                    HandleInput(audioCapture);
                    if(!_isPaused)
                    {
                        currentVisualization = _visualizations[_currentMode];

                        //redraw
                        buffer.Clear();
                        currentVisualization.Draw(buffer);

                        //debug bar
                        if(Config.Config.DEBUG_MODE)
                        {
                            string modeName = Utility.GetModeName(_currentMode);
                            string status = $"MODE:{modeName} | VOL:{audioCapture.SmoothedVolume:F2} | PEAK:{audioCapture.PeakVolume:F2} | {(_isPaused ? "PAUSED" : "RUNNING")} | [TAB] Switch Mode | [SPACE] Pause | [ESC] Exit ";
                            buffer.DrawStatusBar(status, audioCapture.SmoothedVolume);
                        }


                        buffer.Render();
                        Thread.Sleep(Config.Config.FRAME_RATE);
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
                            if(Config.Config.DEBUG_MODE)
                                Console.Beep(1000, 50); //TODO: Convert to use NAudio
                        }
                        break;
                }
            }
        }
    }
}
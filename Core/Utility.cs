using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Core;
using NAudio.Wave;

namespace TERMINAL_FREQUENCY.Core
{
    public static class Utility
    {
        
        public static void PrintStartup()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
    ╔════════════════════════════════════════════════════════╗
    ║                                                        ║
    ║            T E R M I N A L   F R E Q U E N C Y         ║
    ║                                                        ║
    ║           ████████╗███████╗██████╗ ███╗   ███╗         ║
    ║           ╚══██╔══╝██╔════╝██╔══██╗████╗ ████║         ║
    ║              ██║   █████╗  ██████╔╝██╔████╔██║         ║
    ║              ██║   ██╔══╝  ██╔══██╗██║╚██╔╝██║         ║
    ║              ██║   ███████╗██║  ██║██║ ╚═╝ ██║         ║
    ║              ╚═╝   ╚══════╝╚═╝  ╚═╝╚═╝     ╚═╝         ║
    ║                                                        ║
    ║              ███████╗██████╗ ███████╗ ██████╗          ║
    ║              ██╔════╝██╔══██╗██╔════╝██╔═══██╗         ║
    ║              █████╗  ██████╔╝█████╗  ██║   ██║         ║
    ║              ██╔══╝  ██╔══██╗██╔══╝  ██║▄▄ ██║         ║
    ║              ██║     ██║  ██║███████╗╚██████╔╝         ║
    ║              ╚═╝     ╚═╝  ╚═╝╚══════╝ ╚══▀▀═╝          ║
    ║                                                        ║
    ║               Terminal Audio Visualizer v1.0           ║
    ║             github.com/lethargiesleeps/term-freq       ║
    ╚════════════════════════════════════════════════════════╝
    ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nControls:");
            Console.WriteLine("  TAB: Cycle visualization modes");
            Console.WriteLine("  SPACE: Pause/Resume");
            Console.WriteLine("  ESC: Exit");
            Console.WriteLine("--------------------------------");
            Console.WriteLine("  Modify the JSON file to change settings");
            Console.WriteLine("  Press any key to continue :)");
            Console.ReadKey();

        }

        public static AudioCapture SelectAudioDevice()
        {
            Console.WriteLine("\nPlease select an audio device to capture...");
            Console.WriteLine("--------------------------------");
            Console.WriteLine("\nAvailable Audio Input Devices:\n");
            Console.WriteLine("--------------------------------");

            var devices = GetAvailableDevices();

            if (devices.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("No audio input devices found");
                Console.ResetColor();
                return null;
            }

            for (int i = 0; i < devices.Count; i++)
                Console.WriteLine($"  [{i}] {devices[i]}");
            

            Console.WriteLine("\nSelect device number (or press ENTER for default): ");
            string input = Console.ReadLine();

            int selectedIndex = -1;
            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Using default device...");
                return new AudioCapture(); // Use default device
            }

            if (int.TryParse(input, out selectedIndex) && selectedIndex >= 0 && selectedIndex < devices.Count)
            {
                Console.WriteLine($"Selected: {devices[selectedIndex]}");
                return new AudioCapture(); //TODO: Let user select audio device
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid selection. Using default device...");
                Console.ResetColor();
                return new AudioCapture();
            }
        }

        public static string GetModeName(int modeIndex)
        {
            return modeIndex switch
            {
                0 => "RINGS",
                1 => "WATERFALL",
                // 2 => "WAVEFORM",
                // 3 => "EQUALIZER",
                _ => "UNKNOWN"
            };
        }

        public static List<string> GetAvailableDevices()
        {
            List<string> devices = new List<string>();
            try
            {
                for (int i = 0; i < WaveInEvent.DeviceCount; i++)
                    devices.Add($"{WaveInEvent.GetCapabilities(i).ProductName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error enumerating devices: {ex.Message}");
            }

            return devices;
        }

        public static ConsoleColor DarkenColor(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Red => ConsoleColor.DarkRed,
                ConsoleColor.Yellow => ConsoleColor.DarkYellow,
                ConsoleColor.Green => ConsoleColor.DarkGreen,
                ConsoleColor.Cyan => ConsoleColor.DarkCyan,
                ConsoleColor.Blue => ConsoleColor.DarkBlue,
                ConsoleColor.Magenta => ConsoleColor.DarkMagenta,
                ConsoleColor.DarkRed => ConsoleColor.DarkRed,
                ConsoleColor.DarkYellow => ConsoleColor.DarkYellow,
                ConsoleColor.DarkGreen => ConsoleColor.DarkGreen,
                ConsoleColor.DarkCyan => ConsoleColor.DarkCyan,
                ConsoleColor.DarkBlue => ConsoleColor.DarkBlue,
                ConsoleColor.DarkMagenta => ConsoleColor.DarkMagenta,
                _ => ConsoleColor.DarkGray
            };
        }
    }
}

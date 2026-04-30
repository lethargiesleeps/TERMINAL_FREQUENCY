using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Core;
using NAudio.Wave;
using TERMINAL_FREQUENCY.Config;
using TERMINAL_FREQUENCY.Visualization.Shape;

namespace TERMINAL_FREQUENCY.Core
{
    public static class Utility
    {
        
        public static void PrintStartup()
        {
            Console.Clear();
            Console.ForegroundColor = Config.Config.DARK_MODE ? ConsoleColor.DarkMagenta : ConsoleColor.DarkCyan;
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
            Console.ForegroundColor = Config.Config.DARK_MODE ?  ConsoleColor.Gray : ConsoleColor.DarkGray;
            Console.WriteLine("\nControls:");
            Console.WriteLine("  TAB: Cycle visualization modes");
            Console.WriteLine("  D: Toggle Debug");
            Console.WriteLine("  SPACE: Pause/Resume");
            Console.WriteLine("  L: Freeze Controls");
            Console.WriteLine("  ESC: Exit");
            Console.WriteLine("--------------------------------");
            Console.WriteLine("  Modify the JSON file to change settings");
            Console.WriteLine("  Press any key to continue :)");
            Console.ReadKey();

        }

        public static void PrintPause(ScreenBuffer buffer, string modeName)
        {
            string[] lines = new string[]
            {
                "╔═══════════════════════════════════════════════════════════════╗",
                "║                                                               ║",
                "║              T E R M I N A L   F R E Q U E N C Y              ║",
                "║                                                               ║",
                "║       ██████╗  █████╗ ██╗   ██╗███████╗███████╗██████╗        ║",
                "║       ██╔══██╗██╔══██╗██║   ██║██╔════╝██╔════╝██╔══██╗       ║",
                "║       ██████╔╝███████║██║   ██║███████╗█████╗  ██║  ██║       ║",
                "║       ██╔═══╝ ██╔══██║██║   ██║╚════██║██╔══╝  ██║  ██║       ║",
                "║       ██║     ██║  ██║╚██████╔╝███████║███████╗██████╔╝       ║",
                "║       ╚═╝     ╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚══════╝╚═════╝        ║",
                "║                                                               ║",
                "║                    [SPACE] Resume  [ESC] Exit                 ║",
                "╚═══════════════════════════════════════════════════════════════╝",
                $"CURRENT MODE: {modeName}"

            };

            int boxWidth = lines[0].Length;
            int boxHeight = lines.Length;

            int startX = Math.Max(0, (buffer.Width - boxWidth) / 2);
            int startY = Math.Max(0, (buffer.Height - boxHeight) / 2);

            for (int y = 0; y < boxHeight; y++)
                for (int x = 0; x < lines[y].Length; x++)
                    if (startX + x < buffer.Width && startY + y < buffer.Height)
                        buffer.SetPixel(startX + x, startY + y, lines[y][x], ConsoleColor.DarkMagenta);
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
                2 => "SHAPE",
                // 3 => "EQUALIZER",
                _ => "UNKNOWN"
            };
        }

        public static string FormatEnum(Enum value)
        {
            if(value is ConsoleColor color)
            {
                return color switch 
                {
                    ConsoleColor.Black => "BLACK",
                    ConsoleColor.White => "WHITE",
                    ConsoleColor.Red => "RED",
                    ConsoleColor.Blue => "BLUE",
                    ConsoleColor.Green => "GREEN",
                    ConsoleColor.Yellow => "YLLOW",
                    ConsoleColor.Cyan => "CYAN",
                    ConsoleColor.Magenta => "MGNTA",
                    ConsoleColor.Gray => "GRAY",
                    ConsoleColor.DarkRed => "DRED",
                    ConsoleColor.DarkBlue => "DBLUE",
                    ConsoleColor.DarkGreen => "DGRN",
                    ConsoleColor.DarkYellow => "DYLLW",
                    ConsoleColor.DarkCyan => "DCYAN",
                    ConsoleColor.DarkMagenta => "DMGNT",
                    ConsoleColor.DarkGray => "DGRAY",
                    _ => "???"
                };
            }

            if(value is VisualizationOrigin origin)
            {
                return origin switch
                {
                    VisualizationOrigin.Center => "CNTR",
                    VisualizationOrigin.Top => "TOP",
                    VisualizationOrigin.Right => "RIGHT",
                    VisualizationOrigin.Bottom => "BTTM",
                    VisualizationOrigin.Left => "LEFT",
                    _ => "???"
                };
            }

            if(value is ColorMode colorMode)
            {
                return colorMode switch
                {
                    ColorMode.All => "ALL",
                    ColorMode.Light => "LIGHT",
                    ColorMode.Dark => "DARK",
                    ColorMode.Red => "RED",
                    ColorMode.Blue => "BLUE",
                    ColorMode.Green => "GREEN",
                    ColorMode.Yellow => "YLLOW",
                    ColorMode.RainbowDark => "DRNBW",
                    ColorMode.RainbowLight => "RNBW",
                    ColorMode.Random => "RNDM",
                    _ => "???"
                };
            }

            if(value is ShapeLayout shapeLayout)
            {
                return shapeLayout switch
                {
                    ShapeLayout.Single => "SINGL",
                    ShapeLayout.Vertical => "VERT",
                    ShapeLayout.Horizontal => "HORZ",
                    ShapeLayout.Pyramid => "PYRMD",
                    ShapeLayout.Quadrant => "QDRNT",
                    ShapeLayout.Concentric => "CONCT",
                    _ => "???"
                };
            }

            if(value is ShapeType shapeType)
            {
                return shapeType switch
                {
                    ShapeType.Circle => "CRCL",
                    ShapeType.Square => "SQR",
                    ShapeType.Diamond => "DMND",
                    ShapeType.Polygon => "POLY",
                    ShapeType.TriangleUp => "TRI1",
                    ShapeType.TriangleDown => "TRI2",
                    _ => "???"
                };
            }

            if(value is WaterfallMode waterfallMode)
            {
                return waterfallMode switch
                {
                    WaterfallMode.Normal => "NRML",
                    WaterfallMode.Clockwise => "CLK1",
                    WaterfallMode.AntiClockwise => "CLK2",
                    WaterfallMode.TopBottom => "TB",
                    WaterfallMode.LeftRight => "LR",
                    WaterfallMode.All => "ALL",
                    _ => "???"
                };
            }

            //fallback
            return value.ToString().ToUpper();
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

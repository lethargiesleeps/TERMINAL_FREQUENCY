using NAudio.Wave;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Core.Audio;
using TERMINAL_FREQUENCY.Core.Rendering;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Visualization.Shape;
using TERMINAL_FREQUENCY.Visualization.Rings;
using TERMINAL_FREQUENCY.Visualization.Waterfall;
using TERMINAL_FREQUENCY.Visualization.Equalizer;

#nullable disable warnings
namespace TERMINAL_FREQUENCY.Core
{
    /// <summary>
    /// Static utility functions used throughout the program.
    /// Mostly any code that can be reused all across the program.
    /// </summary>
    public static class Utility
    {
        public const string VERSION_NUMBER = "v0.7";
        /// <summary>
        /// String data and console methods for the program launch screen.
        /// </summary>
        public static void PrintStartup()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Magenta;
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
    ║               Terminal Audio Visualizer v0.8           ║
    ║             github.com/lethargiesleeps/term-freq       ║
    ╚════════════════════════════════════════════════════════╝
    ");

            Console.WriteLine("\nCONTROLS:");
            Console.WriteLine("  [TAB] CHANGE VISUALIZATION");
            Console.WriteLine("  [D]EBUG ON/OFF");
            Console.WriteLine("  [SPACE] PAUSE/RESUME | [L]OCK CONTROLS | [F5]FULL SCREEN");
            Console.WriteLine("  [F1] SAVE | [F2] LOAD | [F3] RESTORE DEFAULTS");
            Console.WriteLine("  [ESC] EXIT");
            Console.WriteLine("--------------------------------");
            Console.WriteLine("  Modify the JSON file to change settings");
            Console.WriteLine("  Press any key to continue :)");
            Console.ReadKey();

        }

        /// <summary>
        /// String data for when the user paused the screen.
        /// </summary>
        /// <param name="buffer">The main ScreenBuffer being used during program runtime.</param>
        /// <param name="modeName">Name of the current Visualization. </param>
        public static void PrintPause(ScreenBuffer buffer, string modeName)
        {
            string[] lines = new string[]
            {
                "╔══════════════════════════════════════════════════════════════════════════════════════════╗",
                "║                                                                                          ║",
                "║                              T E R M I N A L   F R E Q U E N C Y                         ║",
                "║                                                                                          ║",
                "║                       ██████╗  █████╗ ██╗   ██╗███████╗███████╗██████╗                   ║",
                "║                       ██╔══██╗██╔══██╗██║   ██║██╔════╝██╔════╝██╔══██╗                  ║",
                "║                       ██████╔╝███████║██║   ██║███████╗█████╗  ██║  ██║                  ║",
                "║                       ██╔═══╝ ██╔══██║██║   ██║╚════██║██╔══╝  ██║  ██║                  ║",
                "║                       ██║     ██║  ██║╚██████╔╝███████║███████╗██████╔╝                  ║",
                "║                       ╚═╝     ╚═╝  ╚═╝ ╚═════╝ ╚══════╝╚══════╝╚═════╝                   ║",
                "║                                                                                          ║",
                "║                    [SPACE] Resume  [ESC] Exit  [M] CHANGE RENDERING MODE                 ║",
                "╚══════════════════════════════════════════════════════════════════════════════════════════╝",
                $"CURRENT MODE: {modeName} RENDERER: {buffer.GetRendererMode()}"

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


        /// <summary>
        /// Allows user to select audio device/interface to capture.
        /// </summary>
        /// <returns>The selected audio device/interface.</returns>
        /// <remarks>NOT FULLY IMPLEMENTED</remarks>
        public static AudioCapture? SelectAudioDevice()
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
                return new AudioCapture(new Config.Settings.Settings()); // Use default device
            }

            if (int.TryParse(input, out selectedIndex) && selectedIndex >= 0 && selectedIndex < devices.Count)
            {
                Console.WriteLine($"Selected: {devices[selectedIndex]}");
                return new AudioCapture(new Config.Settings.Settings()); //TODO: Let user select audio device
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Invalid selection. Using default device...");
                Console.ResetColor();
                return new AudioCapture(new Config.Settings.Settings());
            }
        }


        public static string GetModeName(int modeIndex)
        {
            return modeIndex switch
            {
                0 => "RINGS",
                1 => "WATERFALL",
                2 => "SHAPE",
                3 => "EQ",
                _ => "UNKNOWN"
            };
        }

        public static int ByteConstraintsCheck(int value)
        {
            if (value < byte.MinValue) return byte.MinValue;
            else if (value > byte.MaxValue) return byte.MaxValue;
            else return value;
        }
        public static int EnumCount<T>(bool returnLastIndex = false) where T : Enum
        {
            return returnLastIndex
                ? Enum.GetValues(typeof(T)).Length - 1 
                : Enum.GetValues(typeof (T)).Length;
        }

        /// <summary>
        /// Generates renderable strings of values for all of the program's enums.
        /// </summary>
        /// <param name="value">The enum who's values to print.</param>
        /// <returns>The generated value string.</returns>
        public static string FormatEnum(Enum value)
        {
            if(value is RenderMode renderer)
            {
                return renderer switch
                {
                    RenderMode.PerPixel => "PER PIXEL",
                    RenderMode.DirtyRect => "DIRTY RECT",
                    RenderMode.RowBatched => "ROW BATCHED",
                    RenderMode.DirectWrite => "DIRECT WRITE",
                    _ => "???"
                };
            }

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

            if(value is RingColorMode colorMode)
            {
                return colorMode switch
                {
                    RingColorMode.All => "ALL",
                    RingColorMode.Light => "LIGHT",
                    RingColorMode.Dark => "DARK",
                    RingColorMode.Red => "RED",
                    RingColorMode.Blue => "BLUE",
                    RingColorMode.Green => "GREEN",
                    RingColorMode.Yellow => "YLLOW",
                    RingColorMode.RainbowDark => "DRNBW",
                    RingColorMode.RainbowLight => "RNBW",
                    RingColorMode.Random => "RNDM",
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

        public static List<IVisualization> RefreshVisuals(Settings settings) => new List<IVisualization>() 
        { 
            new Rings(settings), 
            new Waterfall(settings), 
            new Shape(settings) ,
            new Equalizer(settings)
        };

        /// <summary>
        /// Uses WASAPI to get a list of all audio devices on a system.
        /// </summary>
        /// <returns>A list of string containing all the available audio devices.</returns>
        /// <remarks>NOT FULLY IMPLEMENTED</remarks>
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

        /// <summary>
        /// Takes a ConsoleColor and returns it's darker counterpart.
        /// </summary>
        /// <param name="color">The ConsoleColor to darken.</param>
        /// <returns>The darkened ConsoleColor.</returns>
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
                ConsoleColor.Gray => ConsoleColor.DarkGray,
                ConsoleColor.White => ConsoleColor.Black,
                _ => color
            };
        }


        /// <summary>
        /// Takes a ConsoleColor and returns it's lighter counterpart.
        /// </summary>
        /// <param name="color">The ConsoleColor to lighten.</param>
        /// <returns>The lightened ConsoleColor.</returns>
        public static ConsoleColor LightenColor(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.DarkRed => ConsoleColor.Red,
                ConsoleColor.DarkYellow => ConsoleColor.Yellow,
                ConsoleColor.DarkGreen => ConsoleColor.Green,
                ConsoleColor.DarkCyan => ConsoleColor.Cyan,
                ConsoleColor.DarkBlue => ConsoleColor.Blue,
                ConsoleColor.DarkMagenta => ConsoleColor.Magenta,
                ConsoleColor.DarkGray => ConsoleColor.Gray,
                ConsoleColor.Black => ConsoleColor.Black,
                _ => color
            };
        }

        #region ArrayCycleHandling
        /// <summary>
        /// Cycles to next value in a predefined array.
        /// </summary>
        /// <typeparam name="T">the type of elements in the array. Can be any type that supports equality comparison.</typeparam>
        /// <param name="values">The array to cycle through.</param>
        /// <param name="currentValue">The value to find in the array. If not found, returns the first value of the array.</param>
        /// <param name="clamp">If true, does not cycle back to first position of the array.</param>
        /// <returns>The next value in sequence from the array.</returns>
        /// <exception cref="ArgumentException">Throws if values aregument is null or empty.</exception>
        public static T CycleNext<T>(T[] values, T currentValue, bool clamp = false)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty", nameof(values));

            int currentIndex = Array.IndexOf(values, currentValue);

            //start from beginning if value no found
            if (currentIndex < 0)
                return values[0];

            if (currentIndex == values.Length - 1 && clamp)
                return values[currentIndex];

            int nextIndex = (currentIndex + 1) % values.Length;
            return values[nextIndex];
        }

        /// <summary>
        /// Cycles to previous value in a predefined array.
        /// </summary>
        /// <typeparam name="T">the type of elements in the array. Can be any type that supports equality comparison.</typeparam>
        /// <param name="values">The array to cycle through.</param>
        /// <param name="currentValue">The value to find in the array. If not found, returns the first value of the array.</param>
        /// <param name="clamp">If true, does not cycle back to last value of the enum.</param>
        /// <returns>The previous value in sequence from the array.</returns>
        /// <exception cref="ArgumentException">Throws if values aregument is null or empty.</exception>
        public static T CyclePrevious<T>(T[] values, T currentValue, bool clamp = false)
        {
            if (values == null || values.Length == 0)
                throw new ArgumentException("Values array cannot be null or empty", nameof(values));

            int currentIndex = Array.IndexOf(values, currentValue);

            //start from end if value no found
            if (currentIndex < 0)
                return values[values.Length - 1];

            if (currentIndex == 0 && clamp)
                return values[currentIndex];

            int prevIndex = (currentIndex - 1 + values.Length) % values.Length;
            return values[prevIndex];
        }

        /// <summary>
        /// Cycles through an enum and returns the next value by converting it into an array of T enum.
        /// </summary>
        /// <typeparam name="T">The enum type to cycle through.</typeparam>
        /// <param name="currentValue">Current value of argument, used to determine enum type.</param>
        /// <param name="clamp">If true, does not cycle back to first value of the enum.</param>
        /// <returns>The next value in the enum.</returns>
        public static T CycleNextEnum<T>(T currentValue, bool clamp = false) where T : struct, Enum
        {
            T[] values = (T[])Enum.GetValues(typeof(T));
            return CycleNext(values, currentValue, clamp);
        }

        /// <summary>
        /// Cycles through an enum and returns the previous value by converting it into an array of T enum.
        /// </summary>
        /// <typeparam name="T">The enum type to cycle through.</typeparam>
        /// <param name="currentValue">Current value of argument, used to determine enum type.</param>
        /// <param name="clamp">If true, does not cycle back to last value of the enum.</param>
        /// <returns>The previous value in the enum.</returns>
        public static T CyclePreviousEnum<T>(T currentValue, bool clamp = false) where T : struct, Enum
        {
            T[] values = (T[])Enum.GetValues(typeof(T));
            return CyclePrevious(values, currentValue, clamp);
        }
        #endregion

    }
}
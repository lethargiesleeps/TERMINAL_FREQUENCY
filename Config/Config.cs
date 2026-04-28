using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Visualization.Shape;

namespace TERMINAL_FREQUENCY.Config
{
    public static class Config
    {
        #region GlobalSettings
        public static bool FORCE_DEFAULT_SETTINGS = true; //TODO: if true, and a settings.json file is read, it ignores any updates and uses default settings
        public static bool FORCE_SETTINGS_SAFE_RANGES = true; //if true, when reading settings if a value is outside predetermined range it snaps to closest acceptable value
        public static bool LOG_SETTINGS_SAFE_RANGE_ERRORS = true; //if true and FORCE_SETTING_SAFE_RANGES is true, prints all errors of values outside saferanges to the console and closes window on input
        public static int THREAD_RATE = 1; //he higher the slower... approx FPS values are [1 = ~ 1000fps (max speed, max cpu usage, beware!), 8 = ~120fps, 16 = ~60fps, 33 = ~30fps, 50 = ~20fps, 100 = ~10fps] (safe range 8-100)
        public static bool DEBUG_MODE = true; //displays extra info if true
        public static int DEFAULT_MODE = 2; //which visualization to start with [0 = Rings, 1 = Waterfall, 2 = shape]
        public static bool SPECIFY_AUDIO_DEVICE = false; //TODO: lets user select which audio device to capture, not implemented
        public static bool DARK_MODE = true; //TODO: if false, console bg is white and default visuals are black to dark gray, not implemented
        public static ConsoleColor BACKGROUND_COLOR = ConsoleColor.Black; //TODO: bg color of console at launch, not implemented
        public static int INSTANCES = 1; //how many independent window processes to launch

        #endregion

        #region AudioCaptureSettings
        public static int AUDIO_SAMPLE_RESOLUTION = 4; //bytes per sample (typically 4, can be 2 or 4)
        public static float RMS_MULTIPLIER = 100f; //scale RMS to the useable volume, safe range 10-500
        public static float NOISE_GATE_THRESHHOLD = 0.01f; //ignores audio below set volume, higher kills quiet sounds, lower keeps noise. can be used to cut out device 'static/humming' that would trigger a visualization
        public static float SMOOTHING_FACTOR_EXISTING = 0.8f; //existing + incoming is always = to 1, controls how quickly volume reacts to a change (how quickly vol is updated)
        public static float SMOOTHING_FACTOR_INCOMING = 0.2f; //see above
        public static float PEAK_TRACKING_MINIMUM = 0.1f; //range to track peaks, prevents noise from becoming a peak (0.05 to 0.3ish for best results)
        public static float PEAK_DECAY_FACTOR = 0.995f;//higher value = hold peak longer for dramatic effect, lower is more responsive (tested safe range of 0.95 - 0.999)
        public static float SPIKE_VOLUME_MINIMUM = 0.05f; //minimum volume to even consider a reaction (tested safe range 0.01 - 0.2)
        public static float SPIKE_RATIO = 1.15f; //how much louder than calculated volume to trigger spike, lower = more sensitive (safe range 1 - 1.5)
        #endregion

        #region ScreenBufferSettings
        //nothing here yet, probably shouldn't mess with this anyways but we will see
        #endregion

        #region RingSettings
        public static bool RINGS_REVERSE_MODE = false; //if true rings start at max radius and shrink inwards
        public static float RING_RADIUS_MIN = 10f; //minimum radius for reverse mode where rings disappear
        public static float RING_RADIUS = 10f; //starting radius of a ring, the lower the closer to the center of the terminal the ring starts (safe range 1-100)
        public static float RING_RADIUS_MAX = 50f; //max radius a ring reaches before being removed, has to be greater than RING_RADIUS
        public static float RING_LIFETIME = 1.0f; //lifespan of ring measured in normalized units. LIFETIME / FADE_RATE = frames before ring 'dies' (safe range 0.1 - 10ish)
        public static float RING_SPEED = 1f; //how many character units the ring expands outward each update frame, higher = expands faster (safe range 0.1 - 5)
        public static float RING_FADE_RATE = 0.02f; //amount of life subtracted each update frame. Higher values = rings die faster (CANNOT BE 0)
        public static ColorMode RING_COLOR_MODE = ColorMode.Light; //modes of colour of the rings, see ColorMode enum
        public static char[] RING_CHARACTERS = { 'O', 'o', '.' }; //default characters used in rings
        public static bool RING_CHAR_RANDOMIZER = false; //if true, randomly renders a character from RING_CHAR_RANDOMIZER_CHARSET instead of using RING_CHARACTERS
        public static string RING_CHAR_RANDOMIZER_CHARSET = "$!@#%^"; //see above

        public static int RINGS_MAX = 3; //number of rings that CAN appear in the console, doesn't guarantee they will all appear
        public static int RING_SEGMENTS = 24; //how many points make up each ring ( 8 to 60, the lower the blockier, the higher the more circle like)
        public static int RING_AMBIENT_SEGMENTS = 40; //how many points in ambient circle (safe range 8 - 40)
        public static int RING_AMBIENT_DOT_INTRVAL = 4; //draw dot every Nth segment
        public static float RING_AMBIENT_BASE_RADIUS = 5f; //min ambient ring radius (safe range 1 - 15)
        public static float RING_AMBIENT_VOLUME_MULTIPLIER = 3f; //normalized volume affects radius by this much (safe range 1-30)
        public static float RING_AMBIENT_RADIUS_MAX = 20f; //how far the ambient ring goes
        public static float RING_Y_STRETCH = 0.45f; //vertical compression to have better circle in console. messing with this value can result in more Oval or Oblong shapes (safe range 0.2 - 0.8 ish)
        public static bool RINGS_DRAW_CROSSHAIR = true; //if true, draws a crosshair in the center of the console
        public static ConsoleColor RINGS_CROSSHAIR_COLOR = ConsoleColor.Gray; //see above
        public static ConsoleColor RING_AMBIENT_COLOR = ConsoleColor.Gray; //ambient color
        public static char RINGS_CROSSHAIR_CHAR = '+';
        public static int RING_OFFSET = 2; //where in the console is deemed the 'center' for the ring to originate from, 2 is always the true center.
        public static bool RINGS_FIREWORKS_MODE = false; //TODO: if true changes origin point of ring randomly, not yet implemented
    
        #endregion

        #region WaterfallSettings
        public static VisualizationOrigin WATERFALL_ORIGIN = VisualizationOrigin.Top; //where the stream starts from (top, bottom, left, right, center = top since it cant start from the center) see VisualizationOrigin enum
        public static bool WATERFALL_REVERSE_MODE = false; //if true, waterfall always starts at center and expands towards WATERFALL_ORIGIN
        public static WaterfallMode WATERFALL_MODE = WaterfallMode.Normal; //see WaterfallMode enum, normal is just from origin point
        public static float WATERFALL_START_WIDTH_PERCENT = 0.05f; //width of waterfall at origin in percent of console width (safe range 1%-50%)
        public static float WATERFALL_END_WIDTH_PERCENT = 0.8f; //width of waterfall at end of its life in percent of console width, has to be higher than start width (safe range 40%-95%)
        public static float WATERFALL_SPEED = 3.0f; //speed which waterfall progresses across screen (safe range 1 - 10)
        public static float WATERFALL_FADE_RATE = 0.005f; //life lost per frame where 1 represents full life (safe range 0.001 0.05)
        public static int WATERFALL_MAX_STREAMS = 8; //maximum number of waterfall streams before oldest one disappears, the higher the more cpu intensive and the likelier of losing FPS (safe range 1-25)
        public static float WATERFALL_TRIGGER_THRESHOLD = 0.08f; //minimum volume intensity to spawn new waterfall in percentage (safe range 1% to 30%)
        public static bool WATERFALL_ONLY_SPAWN_ON_THRESHOLD = false; //if true, new waterfall only spawns if volume threshold is met
        public static float WATERFALL_MIDPOINT_CHANGE = 0.5f; //progress threshold where character pattern changes in percentage (first transition) (safe range 20% - 80%)
        public static float WATERFALL_ENDPOINT_CHANGE = 0.75f; //progress threshold where character pattern changes in percentage (second transition), has to be higher than midpoint change (safe range 40%-95%)
        public static char[] WATERFALL_VERTICAL_CHARS = new char[3] { '█', '▌', '.' }; //chars rendered on vertical waterfalls (top/bottom origin)
        public static char[] WATERFALL_HORIZONTAL_CHARS = new char[3] { '█', '▌', '.' }; //chars rendered on horizontal waterfalls (left/right origin)
        public static float WATERFALL_CURVE_INTENSITY_VERITCAL = 0.3f; //how pronounced the trailing curve is for vertical waterfalls, 0 = no curve 1 = full curve (range 0 to 1)
        public static float WATERFALL_CURVE_INTENSITY_HORIZONTAL = 0.5f; ////how pronounced the trailing curve is for horizontal waterfalls, 0 = no curve 1 = full curve (range 0 to 1)
        public static char WATERFALL_CURVE_CHAR = '·'; //character used for trailing curve effect
        public static bool WATERFALL_RAINBOW_MODE = false; //if true, each waterfall is a different color without repeating the previous waterfall
        public static float WATERFALL_RAINBOW_FADE_BRIGHT = 0.15f; //white phase end (rainbow mode, only matters if true)
        public static float WATERFALL_RAINBOW_FADE_COLOR = 0.35f;  //full color phase end (rainbow mode, only matters if true)
        public static float WATERFALL_RAINBOW_FADE_DARK = 0.60f;   //darkened color phase end (rainbow mode, only matters if true)
        public static float WATERFALL_RAINBOW_FADE_DARKGRAY = 0.85f; //dark gray phase end (after this = black) (rainbow mode, only matters if true)
        public static float WATERFALL_NORMAL_FADE_WHITE = 0.30f; //white phase end (only if rainbow mode is off)
        public static float WATERFALL_NORMAL_FADE_GRAY = 0.60f; // gray phase end (only if rainbow mode is off)
        public static float WATERFALL_NORMAL_FADE_DARKGRAY = 0.85f; //dark gray phase end (after this = black) (only if rainbow mode is off)
        public static ConsoleColor WATERFALL_COLOR = ConsoleColor.Gray; //which colour to use, default is grey, can't pass white or black.
        #endregion

        #region ShapeSettings
        public static ShapeType SHAPE_TYPE = ShapeType.Square; //which shape gets rendered, see enum
        public static ShapeLayout SHAPE_LAYOUT = ShapeLayout.Single; //how the shape gets laid out (always center if shape count is 1 or shape layout is concentric)
        public static float SHAPE_MAX_SIZE_PERCENT = 0.3f; //how far the shape goes in percentage, must be higher than min (safe range 0.02 to 0.99)
        public static float SHAPE_MIN_SIZE_PERCENT = 0.02f; //size at 0 volume, must be lower than max
        public static int SHAPE_COUNT = 2; //how many shapes get rendered, between 1 and 4
        public static int SHAPE_THICKNESS = 1; //thickness of the outline of the shape
        public static int SHAPE_THICKNESS_MAX = 8; //prevents thickness from dynamically exceeding this value
        public static bool SHAPE_USE_CUSTOM_COLOR = false; //if true, uses SHAPE_CUSTOM_COLORS array
        public static ConsoleColor SHAPE_UNIFORM_COLOR = ConsoleColor.White; //change color here, use custom color if each shape should be different
        public static ConsoleColor[] SHAPE_CUSTOM_COLORS = { ConsoleColor.White, ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Blue }; //used if mode is toggled on, macx of 4
        public static bool SHAPE_REVERSE_MODE = false; //if true, start at max and go inwards for each shape
        public static bool SHAPE_SMOOTH_MODE = true; //if true, attemps to smooth out the shape on motion
        public static float SHAPE_LERP_FACTOR = 0.4f; //smoothing speed for smooth mode
        public static int SHAPE_PADDING = 2; //Chars between concentric shapes
        public static char SHAPE_CHARACTER = '█'; //what prints as the shape
        public static bool SHAPE_VERTICAL_STACK = true; //for count=2: true=vertical, false=horizontal
        public static bool SHAPE_PYRAMID_INVERTED = false; //for pyramid layout, count = 3
        public static float SHAPE_CIRCLE_SEGMENT_DENSITY = 0.7f; //how many points make up the circle relative to its cirumfrance. 1 is one point per radian (super dense)(safe range 0.3 to 1.5)
        public static int SHAPE_CIRCLE_MIN_SEGMENTS = 12; //how 'circular' the circle is, < 12 can result in squares or triangles (safe range 6-20)
        public static int SHAPE_CIRCLE_MAX_SEGMENTS = 120; //affects overall radius, 120 is plenty but can go higher(safe range 60-200)
        public static float SHAPE_SQUARE_WIDTH_RATIO = 1.0f;  //1.0 = perfect square, 0.5 = half width, 2.0 = double width
        public static float SHAPE_SQUARE_HEIGHT_RATIO = 1.0f; //1.0 = perfect square, 0.5 = half height, 2.0 = double height

        public static bool SHAPE_FILL_MODE = false; //if true, fills inside the shape with character, super buggy will fix later, you can make THREAD_RATE really low but beware your CPU usage, works best in a smaller console window
        public static char[] SHAPE_FILL_CHARACTERS = { '░', '▒', '▓', '█' }; //one per shape, if 1 shape then always index 0
        public static ConsoleColor[] SHAPE_FILL_COLORS = { ConsoleColor.DarkGray, ConsoleColor.DarkGray, ConsoleColor.DarkGray, ConsoleColor.DarkGray }; //same as above but for color
        public static int SHAPE_FILL_SPACING = 1; //0 = solid, 1 = every other, 2 = every third
        public static float SHAPE_TRIGGER_THRESHOLD = 0.08f; //ignores volume below this
        #endregion
    }
}

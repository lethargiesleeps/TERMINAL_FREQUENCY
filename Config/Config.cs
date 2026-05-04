using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Config.Font;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Visualization.Rings;
using TERMINAL_FREQUENCY.Visualization.Shape;
using TERMINAL_FREQUENCY.Visualization.Waterfall;

namespace TERMINAL_FREQUENCY.Config
{
    public static class Config
    {


        #region ShapeSettings
        public static ShapeType SHAPE_TYPE = ShapeType.Circle; //which shape gets rendered, see enum
        public static ShapeLayout SHAPE_LAYOUT = ShapeLayout.Single; //how the shape gets laid out (always center if shape count is 1 or shape layout is concentric)

        public static float SHAPE_VOLUME_SENSITIVITY = 0.3f; //use in tandem with TRIGGER_THRESHOLD to effectively clamp the visual and make it less sensitive to louder peaks 1.0 = full, 0.5 = half, 0.1 = barely moves (0.1 - 1)
        public static float SHAPE_TRIGGER_THRESHOLD = 0.15f; //ignores volume below this
        public static float SHAPE_MAX_SIZE_PERCENT = 0.8f; //how far the shape goes in percentage, must be higher than min (safe range 0.02 to 0.99), keep in mind though that the louder the audio might still exceed window bounds on peak
        public static float SHAPE_MIN_SIZE_PERCENT = 0.02f; //size at 0 volume, must be lower than max
        public static int SHAPE_COUNT = 1; //how many shapes get rendered, between 1 and 4
        public static int SHAPE_CONCENTRIC_LAYERS = 1; //how many layers in Concentric mode
        public static int SHAPE_CONENTRIC_PADDING = 2; //Chars between concentric layers
        public static int SHAPE_THICKNESS = 1; //thickness of the outline of the shape
        public static int SHAPE_THICKNESS_MAX = 8; //prevents thickness from dynamically exceeding this value

        public static bool SHAPE_QUADRANT_CENTERED = false; //if true, shapes cluster around center of window, only configured to work if shapes is 4
        public static int[] SHAPE_QUADRANT_INDICES = { }; //empty = auto, else manual quadrants from 0 to 3
        public static int SHAPE_QUADRANT_GAP_DIVISOR = 8; //smaller = wider gap between shapes (safe range 5-20)
        public static bool SHAPE_USE_CUSTOM_COLOR = false; //if true, uses SHAPE_CUSTOM_COLORS array
        public static ConsoleColor SHAPE_UNIFORM_COLOR = ConsoleColor.White; //change color here, use custom color if each shape should be different
        public static ConsoleColor[] SHAPE_CUSTOM_COLORS = { ConsoleColor.White, ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Blue }; //used if mode is toggled on, macx of 4
        
        public static bool SHAPE_REVERSE_MODE = false; //if true, start at max and go inwards for each shape
        public static float SHAPE_REVERSE_VOLUME_SENSITIVITY = 0.05f; //normalizes the threshold so shape get closer to center, the closer to 0 the closer to center the shape will get at max volume (safe range: 0.01 to 0.05ish) 
        public static bool SHAPE_SMOOTH_MODE = true; //if true, attemps to smooth out the shape on motion
        public static float SHAPE_LERP_FACTOR = 0.4f; //smoothing speed for smooth mode
        public static char SHAPE_CHARACTER = '█'; //what prints as the shape
        public static bool SHAPE_VERTICAL_STACK = true; //for count=2: true=vertical, false=horizontal
        
        public static float SHAPE_CIRCLE_SEGMENT_DENSITY = 0.8f; //how many points make up the circle relative to its cirumfrance. 1 is one point per radian (super dense)(safe range 0.3 to 1.5)
        public static int SHAPE_CIRCLE_MIN_SEGMENTS = 12; //how 'circular' the circle is, < 12 can result in squares or triangles (safe range 6-20)
        public static int SHAPE_CIRCLE_MAX_SEGMENTS = 120; //affects overall radius, 120 is plenty but can go higher(safe range 60-200)
        
        public static float SHAPE_SQUARE_WIDTH_RATIO = 1.0f;  //1.0 = perfect square, 0.5 = half width, 2.0 = double width
        public static float SHAPE_SQUARE_HEIGHT_RATIO = 1.0f; //1.0 = perfect square, 0.5 = half height, 2.0 = double height


        public static float SHAPE_TRIANGLE_SIDE_MULTIPLIER = 1.8f;  // Side length relative to radius
        public static float SHAPE_TRIANGLE_HEIGHT_MULTIPLIER = 0.87f; // sqrt(3)/2 for equilateral, adjust for different proportions
        public static float SHAPE_TRIANGLE_ASPECT_CORRECTION = 0.45f; // Console char aspect ratio
        public static float SHAPE_PYRAMID_ROW_SPACING = 0.25f; //space between rows in pyramid layout, higher = more space, lower = less space (safe range 0.08 to 0.3)
        public static int SHAPE_POLYGON_SIDES = 5;//can accept 5, 6, 8, 10, 12 anything higher might as well use circle        
        public static bool SHAPE_FILL_MODE = false; //if true, fills inside the shape with character, super buggy will fix later, you can make THREAD_RATE really low but beware your CPU usage, works best in a smaller console window
        public static char[] SHAPE_FILL_CHARACTERS = new char[4] { '█', '▓', '▒' , '█' }; //one per shape, if 1 shape then always index 0
        public static ConsoleColor[] SHAPE_FILL_COLORS = { ConsoleColor.DarkGray, ConsoleColor.DarkGray, ConsoleColor.DarkGray, ConsoleColor.DarkGray }; //same as above but for color
        public static int SHAPE_FILL_SPACING = 1; //0 = solid, 1 = every other, 2 = every third

        #endregion

        public static void RestoreDefaults()
        {

        }
    }
}

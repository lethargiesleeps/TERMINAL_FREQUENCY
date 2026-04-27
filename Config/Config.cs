using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TERMINAL_FREQUENCY.Config
{
    public static class Config
    {
        #region Global
        public static int FRAME_RATE = 33;
        public static bool DEBUG_MODE = true;
        public static int DEFAULT_MODE = 0;
        public static bool SPECIFY_AUDIO_DEVICE = false;
        public static bool DARK_MODE = true;
        public static ConsoleColor BACKGROUND_COLOR = ConsoleColor.Black;
        #endregion
        #region AudioCapture
        public static int BYTE_4 = 4;
        public static float RMS_CEILING = 100f;
        public static float VOL_FLOOR = 0.01f;
        public static float VOL_CORRECTOR_CEILING = 0.8f;
        public static float VOL_CORRECTOR_FLOOR = 0.2f;
        public static float CLIPPING_THRESHOLD = 0.1f;
        public static float CLIPPING_PREVENTION = 0.995f;
        #endregion

        #region Rings
        public static float RING_RADIUS = 10f;
        public static float RING_RADIUS_MAX = 50;
        public static float RING_LIFETIME = 1.0f;
        public static float RING_UPDATE_SPEED = 0.7f;
        public static float RING_UPDATE_FADE_RATE = 0.5f;
        public static ColorMode RING_COLOR_MODE = ColorMode.Blue;
        public static char[] RING_CHARACTERS = new char[] { '+', 'O', 'o' }; //0 = star, 1 = mid, 2 end
        public static bool USE_RING_CHAR_RANDOMIZER = false;
        public static string RING_CHAR_RANDOMIZER_CHARSET = "$!@#%^";
        public static bool USE_ALPHANUMERIC = false;
        public static bool USE_SYMBOLS = false;
        #endregion

        #region KickCircle
        public static int MAX_RINGS = 5;
        private static float AMBIENT_RADIUS_MAX = 5;
        private static float AMBIENT_RADIUS_SMOOTH_THRESHOLD = 3f;
        public static float SET_AMBIENT_RADIUS(float smoothedVolume) => AMBIENT_RADIUS_MAX + smoothedVolume * AMBIENT_RADIUS_SMOOTH_THRESHOLD;
        public static float AMBIENT_RADIUS_MAX_REACH = 20;
        #endregion
    }
}

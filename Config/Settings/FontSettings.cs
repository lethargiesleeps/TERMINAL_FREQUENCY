using TERMINAL_FREQUENCY.Config.Font;
using TERMINAL_FREQUENCY.Core;

namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class FontSettings : IConfigurable
    {
        public bool EnableRasterFont { get; set; }                //if true, uses a raster font, can be used if some characters are not displaying correctly (appear as different characters or not found character
        public RasterFontType RasterFontType { get; set; }        //which raster font to use
        public bool EnableCustomFont { get; set; }                //allows user to set font settings. enabling raster font takes priority
        public FontFace CustomFontFace { get; set; }              //true type font to use, user can use font face override if their system supports that font
        public string? CustomFontFaceOverride { get; set; }       //if not empty, attemps to set to provided font face
        public int CustomFontSize { get; set; }                   //size of custom font
        public bool CustomFontBold { get; set; }                   //if true, font weight is 700, otherwise 400

        public FontSettings()
        {
            Restore();
        }

        public void Restore()
        {
            EnableRasterFont = true;
            RasterFontType = RasterFontType.EightByTwelve;
            EnableCustomFont = false;
            CustomFontFace = FontFace.Consolas;
            CustomFontFaceOverride = string.Empty;
            CustomFontSize = 16;
            CustomFontBold = false;
        }

        public void EnforceConstraints()
        {
            return; //not necessary here
        }

        public void EnforceMandatoryConstraints()
        {
            if ((int)RasterFontType < 0 || (int)RasterFontType > Utility.EnumCount<RasterFontType>(true))
                RasterFontType = RasterFontType.EightByTwelve;

            if ((int)CustomFontFace < 0 || (int)CustomFontFace > Utility.EnumCount<FontFace>(true))
                CustomFontFace = FontFace.Consolas;

            if (CustomFontSize < 5)
                CustomFontSize = 5;
            else if (CustomFontSize > 72)
                CustomFontSize = 72;
        }
    }
}

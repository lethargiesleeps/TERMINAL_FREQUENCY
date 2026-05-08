using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Visualization.Equalizer;

namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class EqualizerSettings : IConfigurable
    {
        public VisualizationOrigin Origin { get; set; }    //where the equalizer is positioned in the window TODO: Center will have it at each side of the window but this is not implemented
        public EqColorMode ColorMode { get; set; }         //whether the bands are all 1 colour, follow a pattern based on ColorPattern or are a gradient (green, yellow, red)
        public ConsoleColor UniformColor { get; set; }     //color of bands when in uniform color mode
        public ConsoleColor[] ColorPattern { get; set; }   //colors to use when in Pattern color mode. any color added above the number of bands gets ignored, repeats after last color
        public ConsoleColor[] GradientColors { get; set; }  //3 colors to use when in Gradient color mode. must be 3 colors.
        public bool SolidBands { get; set; }                 //if true, bars are filled in, otherwise just the outline shows
        public bool SmoothMode { get; set; }                //if on, smooths band animation instead of snapping to its value via LerpFactor
        public float LerpFactor { get; set; }               //intensity of the smoothing effect.
        public EqDirection Direction { get; set; }          //whether the bands are displayed low to high, high to low, or compressed and reflected from the center (if mirrored, divides band count by 2)
        public char BandCharacter { get; set; }
        public int BandSpacing { get; set; }
        public float MaxBandHeightPercent { get; set; }
        public float MinBandHeightPercent { get; set; }
        public bool HorizontalWhenCentered { get; set; }

        public EqualizerSettings()
        {
            Restore();
        }

        public void EnforceConstraints()
        {
            if (BandSpacing < 0) BandSpacing = 0;
            if (BandSpacing > 5) BandSpacing = 5;
            if (MaxBandHeightPercent < 0.1f) MaxBandHeightPercent = 0.1f;
            if (MaxBandHeightPercent > 0.95f) MaxBandHeightPercent = 0.95f;
            if (MinBandHeightPercent < 0.01f) MinBandHeightPercent = 0.01f;
            if (MinBandHeightPercent > MaxBandHeightPercent) MinBandHeightPercent = MaxBandHeightPercent * 0.1f;
        }

        public void EnforceMandatoryConstraints()
        {
            if(GradientColors.Length != 3) GradientColors = new[] { ConsoleColor.Green, ConsoleColor.Yellow, ConsoleColor.Red };
            if (LerpFactor < 0.01f) LerpFactor = 0.01f;
            if (LerpFactor > 1f) LerpFactor = 1f;
            if ((int)Origin < 0 || (int)Origin > Utility.EnumCount<VisualizationOrigin>(true))
                Origin = VisualizationOrigin.Bottom;
            if ((int)ColorMode < 0 || (int)ColorMode > Utility.EnumCount<EqColorMode>(true))
                ColorMode = EqColorMode.Gradient;
            if ((int)Direction < 0 || (int)Direction > Utility.EnumCount<EqDirection>(true))
                Direction = EqDirection.LowToHigh;

            if ((int)UniformColor < 0 || (int)UniformColor > Utility.EnumCount<ConsoleColor>(true))
                UniformColor = ConsoleColor.White;

            if (ColorPattern == null || ColorPattern.Length == 0)
                ColorPattern = new ConsoleColor[] { ConsoleColor.White, ConsoleColor.Cyan };
            if (MaxBandHeightPercent < 0.01f) MaxBandHeightPercent = 0.01f;
            if (MinBandHeightPercent < 0.01f) MinBandHeightPercent = 0.01f;
            if (MaxBandHeightPercent <= MinBandHeightPercent) MaxBandHeightPercent = MinBandHeightPercent + 0.05f;
            if (MinBandHeightPercent >= MaxBandHeightPercent) MinBandHeightPercent = MaxBandHeightPercent - 0.05f;
        }

        public void Restore()
        {
            Origin = VisualizationOrigin.Bottom;
            ColorMode = EqColorMode.Uniform;
            UniformColor = ConsoleColor.White;
            ColorPattern = new[] { ConsoleColor.White, ConsoleColor.Green };
            GradientColors = new[] { ConsoleColor.Green, ConsoleColor.Yellow, ConsoleColor.Red };
            SolidBands = true;
            SmoothMode = true;
            LerpFactor = 0.4f;
            Direction = EqDirection.LowToHigh;
            BandCharacter = '█';
            BandSpacing = 1;
            MaxBandHeightPercent = 0.99f;
            MinBandHeightPercent = 0.01f;
            HorizontalWhenCentered = false;
        }
    }
}

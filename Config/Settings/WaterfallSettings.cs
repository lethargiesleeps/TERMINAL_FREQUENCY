using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Visualization;
using TERMINAL_FREQUENCY.Visualization.Waterfall;

#nullable disable warnings
namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class WaterfallSettings : IConfigurable
    {
        public VisualizationOrigin Origin { get; set; }              //where the stream starts from (top, bottom, left, right, center = top since it cant start from the center) see VisualizationOrigin enum
        public bool ReverseMode { get; set; }                        //if true, waterfall always starts at center and expands towards WATERFALL_ORIGIN
        public WaterfallMode Mode { get; set; }                      //see WaterfallMode enum, normal is just from origin point
        public float StartWidthPercent { get; set; }                 //width of waterfall at origin in percent of console width (safe range 1%-50%)
        public float EndWidthPercent { get; set; }                   //width of waterfall at end of its life in percent of console width, has to be higher than start width (safe range 40%-95%)
        public float Speed { get; set; }                             //speed which waterfall progresses across screen (safe range 1 - 10)
        public float FadeRate { get; set; }                          //life lost per frame where 1 represents full life (safe range 0.001 0.05)
        public int MaxStreams { get; set; }                          //maximum number of waterfall _streams before oldest one disappears, the higher the more cpu intensive and the likelier of losing FPS (safe range 1-25)
        public float TriggerThreshold { get; set; }                  //minimum volume intensity to spawn new waterfall in percentage (safe range 1% to 30%)
        public bool OnlySpawnOnThreshold { get; set; }               //if true, new waterfall only spawns if volume threshold is met
        public float MidpointChange { get; set; }                    //progress threshold where character pattern changes in percentage (first transition) (safe range 20% - 80%)
        public float EndpointChange { get; set; }                    //progress threshold where character pattern changes in percentage (second transition), has to be higher than midpoint change (safe range 40%-95%)
        public char[] VerticalChars { get; set; }                    //chars rendered on vertical waterfalls (top/bottom origin)
        public char[] HorizontalChars { get; set; }                  //chars rendered on horizontal waterfalls (left/right origin)
        public float CurveIntensityVertical { get; set; }            //how pronounced the trailing curve is for vertical waterfalls, 0 = no curve 1 = full curve (range 0 to 1)
        public float CurveIntensityHorizontal { get; set; }          //how pronounced the trailing curve is for horizontal waterfalls, 0 = no curve 1 = full curve (range 0 to 1)
        public char CurveChar { get; set; }                          //character used for trailing curve effect
        public bool RainbowMode { get; set; }                        //if true, each waterfall is a different color without repeating the previous waterfall
        public float RainbowFadeBright { get; set; }                 //white phase end (rainbow mode, only matters if true)
        public float RainbowFadeColor { get; set; }                  //full color phase end (rainbow mode, only matters if true)
        public float RainbowFadeDark { get; set; }                   //darkened color phase end (rainbow mode, only matters if true)
        public float RainbowFadeDarkGray { get; set; }               //dark gray phase end (after this = black) (rainbow mode, only matters if true)
        public float NormalFadeWhite { get; set; }                   //white phase end (only if rainbow mode is off)
        public float NormalFadeGray { get; set; }                    // gray phase end (only if rainbow mode is off)
        public float NormalFadeDarkGray { get; set; }                //dark gray phase end (after this = black) (only if rainbow mode is off)
        public ConsoleColor Color { get; set; }                      //which colour to use, default is grey, can't pass white or black.

        public WaterfallSettings()
        {
            Restore();
        }

        public void Restore()
        {
            Origin = VisualizationOrigin.Top;
            ReverseMode = false;
            Mode = WaterfallMode.Normal;
            StartWidthPercent = 0.05f;
            EndWidthPercent = 0.9f;
            Speed = 1.0f;
            FadeRate = 0.005f;
            MaxStreams = 10;
            TriggerThreshold = 0.08f;
            OnlySpawnOnThreshold = false;
            MidpointChange = 0.5f;
            EndpointChange = 0.75f;
            VerticalChars = new char[] { '█', '▓', '▒' };
            HorizontalChars = new char[] { '█', '▓', '▒' };
            CurveIntensityVertical = 0.3f;
            CurveIntensityHorizontal = 0.5f;
            CurveChar = '=';
            RainbowMode = false;
            RainbowFadeBright = 0.15f;
            RainbowFadeColor = 0.35f;
            RainbowFadeDark = 0.60f;
            RainbowFadeDarkGray = 0.85f;
            NormalFadeWhite = 0.30f;
            NormalFadeGray = 0.60f;
            NormalFadeDarkGray = 0.85f;
            Color = ConsoleColor.Gray;
        }

        public void EnforceConstraints()
        {
            if (StartWidthPercent < 0.01f) StartWidthPercent = 0.01f;
            if (StartWidthPercent > 0.50f) StartWidthPercent = 0.50f;
            if (EndWidthPercent < 0.40f) EndWidthPercent = 0.40f;
            if (EndWidthPercent > 0.95f) EndWidthPercent = 0.95f;
            if (EndWidthPercent <= StartWidthPercent) EndWidthPercent = StartWidthPercent + 0.1f;
            if (Speed < 1f) Speed = 1f;
            if (Speed > 20f) Speed = 20f;
            if (FadeRate < 0.001f) FadeRate = 0.001f;
            if (FadeRate > 0.05f) FadeRate = 0.05f;
            if (MaxStreams < 1) MaxStreams = 1;
            if (MaxStreams > 25) MaxStreams = 25;
            if (TriggerThreshold < 0.01f) TriggerThreshold = 0.01f;
            if (TriggerThreshold > 0.30f) TriggerThreshold = 0.30f;
            if (MidpointChange < 0.20f) MidpointChange = 0.20f;
            if (MidpointChange > 0.80f) MidpointChange = 0.80f;
            if (EndpointChange < 0.40f) EndpointChange = 0.40f;
            if (EndpointChange > 0.95f) EndpointChange = 0.95f;
            if (EndpointChange <= MidpointChange) EndpointChange = MidpointChange + 0.1f;
            if (CurveIntensityVertical < 0f) CurveIntensityVertical = 0f;
            if (CurveIntensityVertical > 1f) CurveIntensityVertical = 1f;
            if (CurveIntensityHorizontal < 0f) CurveIntensityHorizontal = 0f;
            if (CurveIntensityHorizontal > 1f) CurveIntensityHorizontal = 1f;

            if (RainbowFadeBright > 1f) RainbowFadeBright = 1f;
            if (RainbowFadeColor > 1f) RainbowFadeColor = 1f;
            if (RainbowFadeDark > 1f) RainbowFadeDark = 1f;
            if (RainbowFadeDarkGray > 1f) RainbowFadeDarkGray = 1f;

            if (NormalFadeWhite > 1f) NormalFadeWhite = 1f;
            if (NormalFadeGray > 1f) NormalFadeGray = 1f;
            if (NormalFadeDarkGray > 1f) NormalFadeDarkGray = 1f;
        }

        public void EnforceMandatoryConstraints()
        {
            if (StartWidthPercent < 0f) StartWidthPercent = 0.01f;
            if (EndWidthPercent < 0f) EndWidthPercent = 0.01f;
            if (Speed < 0.01f) Speed = 0.01f;
            if (FadeRate < 0f) FadeRate = 0.001f;
            if (MaxStreams < 1) MaxStreams = 1;
            if (TriggerThreshold < 0f) TriggerThreshold = 0.01f;

            if (MidpointChange < 0f) MidpointChange = 0.01f;
            if (EndpointChange < 0f) EndpointChange = 0f;
            if (EndpointChange <= MidpointChange) EndpointChange = MidpointChange + 0.01f;

            if (CurveIntensityVertical < 0f) CurveIntensityVertical = 0f;
            if (CurveIntensityHorizontal < 0f) CurveIntensityHorizontal = 0f;

            if (RainbowFadeBright < 0f) RainbowFadeBright = 0.01f;
            if (RainbowFadeColor < 0f) RainbowFadeColor = RainbowFadeBright + 0.01f;
            if (RainbowFadeDark < 0f) RainbowFadeDark = RainbowFadeColor + 0.01f;
            if (RainbowFadeDarkGray < 0f) RainbowFadeDarkGray = RainbowFadeDark + 0.01f;
            if (NormalFadeWhite < 0f) NormalFadeWhite = 0.01f;
            if (NormalFadeGray < 0f) NormalFadeGray = NormalFadeWhite + 0.01f;
            if (NormalFadeDarkGray < 0f) NormalFadeDarkGray = NormalFadeGray + 0.01f;

            if ((int)Origin < 0 || (int)Origin > Utility.EnumCount<VisualizationOrigin>(true))
                Origin = VisualizationOrigin.Top;
            if ((int)Mode < 0 || (int)Mode > Utility.EnumCount<WaterfallMode>(true))
                Mode = WaterfallMode.Normal;
            if ((int)Color < 0 || (int)Color > Utility.EnumCount<ConsoleColor>(true))
                Color = ConsoleColor.Gray;

            if (VerticalChars is null || VerticalChars.Length == 0) VerticalChars = new char[3] { '█', '▓', '▒' };
            if (HorizontalChars is null || HorizontalChars.Length == 0) HorizontalChars = new char[3] { '█', '▓', '▒' };
        }
    }
}

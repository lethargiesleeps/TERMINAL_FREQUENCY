using TERMINAL_FREQUENCY.Config;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Visualization.Rings;

#nullable disable warnings
public class RingsSettings : IConfigurable
{
    public bool ReverseMode { get; set; }                         //if true rings start at max radius and shrink inwards
    public float RadiusMin { get; set; }                          //minimum radius for reverse mode where rings disappear
    public float Radius { get; set; }                             //starting radius of a ring, the lower the closer to the center of the terminal the ring starts (safe range 1-350)
    public float RadiusMax { get; set; }                          //max radius a ring reaches before being removed, has to be greater than RING_RADIUS
    public float Lifetime { get; set; }                           //lifespan of ring measured in normalized units. LIFETIME / FADE_RATE = frames before ring 'dies' (safe range 0.1 - 10ish)
    public float Speed { get; set; }                              //how many character units the ring expands outward each update frame, higher = expands faster (safe range 0.1 - 5)
    public float FadeRate { get; set; }                           //amount of life subtracted each update frame. Higher values = rings die faster (CANNOT BE 0)
    public RingColorMode ColorMode { get; set; }                  //modes of colour of the rings, see ColorMode enum
    public bool SolidColor { get; set; }                          //if true, colour is always the same unless in rainbow mode
    public char[] Characters { get; set; }                        //default characters used in rings
    public bool CharRandomizer { get; set; }                      //if true, randomly renders a character from RING_CHAR_RANDOMIZER_CHARSET instead of using RING_CHARACTERS
    public string CharRandomizerCharset { get; set; }             //see above
    public int MaxRings { get; set; }                             //number of rings that CAN appear in the console, doesn't guarantee they will all appear
    public int Segments { get; set; }                             //how many points make up each ring ( 8 to 80, the lower the blockier, the higher the more circle like)
    public int AmbientSegments { get; set; }                      //how many points in ambient circle (safe range 8 - 40)
    public int AmbientDotInterval { get; set; }                   //draw dot every Nth segment
    public float AmbientBaseRadius { get; set; }                  //min ambient ring radius (safe range 1 - 15)
    public float AmbientVolumeMultiplier { get; set; }            //normalized volume affects radius by this much (safe range 1-30)
    public float AmbientRadiusMax { get; set; }                   //how far the ambient ring goes
    public float YStretch { get; set; }                           //vertical compression to have better circle in console. messing with this value can result in more Oval or Oblong shapes (safe range 0.2 - 0.8 ish)
    public bool DrawCrosshair { get; set; }                       //if true, draws a crosshair in the center of the console
    public ConsoleColor CrosshairColor { get; set; }              //see above
    public ConsoleColor CrosshairRingColor { get; set; }          //ambient color
    public char CrosshairChar { get; set; }                       //character shown in middle
    public char CrosshairCharOutter { get; set; }                 //character shown around cross hair middle
    public int Offset { get; set; }                               //where in the console is deemed the 'center' for the ring to originate from, 2 is always the true center.
    public bool FireworksMode { get; set; }                       //TODO: if true changes origin point of ring randomly, not yet implemented

    public RingsSettings()
    {
        Restore();
    }

    public void Restore()
    {
        ReverseMode = false;
        RadiusMin = 50f;
        Radius = 5f;
        RadiusMax = 50f;
        Lifetime = 1.0f;
        Speed = 1f;
        FadeRate = 0.02f;
        ColorMode = RingColorMode.Light;
        Characters = new char[] { 'O', 'o', '.' };
        CharRandomizer = false;
        CharRandomizerCharset = "1234567890";
        MaxRings = 8;
        Segments = 80;
        AmbientSegments = 40;
        AmbientDotInterval = 4;
        AmbientBaseRadius = 5f;
        AmbientVolumeMultiplier = 3f;
        AmbientRadiusMax = 20f;
        YStretch = 0.45f;
        DrawCrosshair = false;
        CrosshairColor = ConsoleColor.Gray;
        CrosshairRingColor = ConsoleColor.Gray;
        CrosshairChar = '+';
        CrosshairCharOutter = '.';
        Offset = 2;
        FireworksMode = false;
        SolidColor = true;
    }

    public void EnforceConstraints()
    {
        if(ReverseMode)
        {
            if (RadiusMin < 1f) RadiusMin = 1f;
            if (RadiusMin > 100f) RadiusMin = 100f;
        }

        if (Radius < 1f) Radius = 1f;
        if (Radius > 500f) Radius = 500f;

        if (Speed < 0.1f) Speed = 0.1f;
        if (Speed > 5f) Speed = 5f;

        if (FadeRate <= 0f) FadeRate = 0.01f;
        if (MaxRings < 1) MaxRings = 1;
        if (MaxRings > 15) MaxRings = 15;
        if (Segments < 5 ) Segments = 5;
        if (Segments > 100) Segments = 100;
        if (AmbientSegments < 5) AmbientSegments = 5;
        if (AmbientSegments > 40) AmbientSegments = 40;
        if (AmbientBaseRadius < 1) AmbientBaseRadius = 1f;
        if (AmbientBaseRadius > 15) AmbientBaseRadius = 15f;

        if (AmbientVolumeMultiplier < 1f) AmbientVolumeMultiplier = 1f;
        if (AmbientVolumeMultiplier > 30f) AmbientVolumeMultiplier = 30f;

        if (YStretch < 0.2f) YStretch = 0.2f;
        if (YStretch > 0.9f) YStretch = 0.9f;

    }

    public void EnforceMandatoryConstraints()
    {
        if (RadiusMin < 0.01f) RadiusMin = 0.01f;
        if (Radius < 0.01f) Radius = 0.01f;
        if (RadiusMax < 0.01f) RadiusMax = 0.01f;
        if (RadiusMax <= Radius) RadiusMax = Radius + 5f;
        if (Lifetime < 0.0001f) Lifetime = 0.0001f;
        if (Speed < 0.001f) Speed = 0.001f;
        if (FadeRate <= 0f) FadeRate = 0.01f;
        if (MaxRings < 1) MaxRings = 1;
        if (Segments < 1) Segments = 1;
        if (AmbientSegments < 1) AmbientSegments = 1;
        if (AmbientDotInterval < 1) AmbientDotInterval = 1;
        if (AmbientBaseRadius < 0.01f) AmbientBaseRadius = 0.01f;
        if (AmbientVolumeMultiplier < 0.01f) AmbientVolumeMultiplier = 0.01f;
        if (AmbientRadiusMax < 0.01f) AmbientRadiusMax = 0.01f;
        if (YStretch < 0.01f) YStretch = 0.01f;
        if (Offset < 0) Offset = 0;

        if ((int)ColorMode < 0 || (int)ColorMode > Utility.EnumCount<RingColorMode>(true))
            ColorMode = RingColorMode.Light;
        if ((int)CrosshairColor < 0 || (int)CrosshairColor > Utility.EnumCount<ConsoleColor>(true))
            CrosshairColor = ConsoleColor.Gray;
        if ((int)CrosshairRingColor < 0 || (int)CrosshairRingColor > Utility.EnumCount<ConsoleColor>(true))
            CrosshairRingColor = ConsoleColor.Gray;

        if(Characters is null || Characters.Length <= 0) Characters = new char[] { 'O', 'o', '.' };
    }
}
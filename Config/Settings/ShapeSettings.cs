using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Visualization.Shape;

#nullable disable warnings
namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class ShapeSettings : IConfigurable
    {
        public ShapeType Type { get; set; }                              //which shape gets rendered, see enum
        public ShapeLayout Layout { get; set; }                          //how the shape gets laid out (always center if shape count is 1 or shape layout is concentric)
        public float VolumeSensitivity { get; set; }                     //use in tandem with TRIGGER_THRESHOLD to effectively clamp the visual and make it less sensitive to louder peaks 1.0 = full, 0.5 = half, 0.1 = barely moves (0.1 - 1)
        public float TriggerThreshold { get; set; }                      //ignores volume below this
        public float MaxSizePercent { get; set; }                        //how far the shape goes in percentage, must be higher than min (safe range 0.02 to 0.99), keep in mind though that the louder the audio might still exceed window bounds on peak
        public float MinSizePercent { get; set; }                        //size at 0 volume, must be lower than max
        public int Count { get; set; }                                   //how many shapes get rendered, between 1 and 4
        public int ConcentricLayers { get; set; }                        //how many layers in Concentric mode
        public int ConcentricPadding { get; set; }                       //Chars between concentric layers
        public int Thickness { get; set; }                               //thickness of the outline of the shape
        public int ThicknessMax { get; set; }                            //prevents thickness from dynamically exceeding this value
        public bool QuadrantCentered { get; set; }                       //if true, shapes cluster around center of window, only configured to work if shapes is 4
        public int[] QuadrantIndices { get; set; }                       //empty = auto, else manual quadrants from 0 to 3
        public int QuadrantGapDivisor { get; set; }                      //smaller = wider gap between shapes (safe range 5-20)
        public bool UseCustomColor { get; set; }                         //if true, uses SHAPE_CUSTOM_COLORS array
        public ConsoleColor UniformColor { get; set; }                   //change color here, use custom color if each shape should be different
        public ConsoleColor[] CustomColors { get; set; }                 //used if mode is toggled on, macx of 4
        public bool ReverseMode { get; set; }                            //if true, start at max and go inwards for each shape
        public float ReverseVolumeSensitivity { get; set; }              //normalizes the threshold so shape get closer to center, the closer to 0 the closer to center the shape will get at max volume (safe range: 0.01 to 0.05ish) 
        public bool SmoothMode { get; set; }                             //if true, attemps to smooth out the shape on motion
        public float LerpFactor { get; set; }                            //smoothing speed for smooth mode
        public char Character { get; set; }                              //what prints as the shape
        public bool VerticalStack { get; set; }                          //for count=2: true=vertical, false=horizontal
        public float CircleSegmentDensity { get; set; }                  //how many points make up the circle relative to its cirumfrance. 1 is one point per radian (super dense)(safe range 0.3 to 1.5)
        public int CircleMinSegments { get; set; }                       //how 'circular' the circle is, < 12 can result in squares or triangles (safe range 6-20)
        public int CircleMaxSegments { get; set; }                       //affects overall radius, 120 is plenty but can go higher(safe range 60-200)
        public float SquareWidthRatio { get; set; }                      //1.0 = perfect square, 0.5 = half width, 2.0 = double width
        public float SquareHeightRatio { get; set; }                     //1.0 = perfect square, 0.5 = half height, 2.0 = double height
        public float TriangleSideMultiplier { get; set; }                // Side length relative to radius
        public float TriangleHeightMultiplier { get; set; }              // sqrt(3)/2 for equilateral, adjust for different proportions
        public float TriangleAspectCorrection { get; set; }              // Console char aspect ratio
        public float PyramidRowSpacing { get; set; }                     //space between rows in pyramid layout, higher = more space, lower = less space (safe range 0.08 to 0.3)
        public int PolygonSides { get; set; }                            //can accept 5, 6, 8, 10, 12 anything higher might as well use circle        
        public bool FillMode { get; set; }                               //if true, fills inside the shape with character, super buggy will fix later, you can make THREAD_RATE really low but beware your CPU usage, works best in a smaller console window
        public int FillSpacing { get; set; }                             //0 = solid, 1 = every other, 2 = every third

        public ShapeSettings()
        {
            Restore();
        }

        public void Restore()
        {
            Type = ShapeType.Circle;
            Layout = ShapeLayout.Single;
            VolumeSensitivity = 0.3f;
            TriggerThreshold = 0.15f;
            MaxSizePercent = 0.8f;
            MinSizePercent = 0.02f;
            Count = 1;
            ConcentricLayers = 1;
            ConcentricPadding = 2;
            Thickness = 4;
            ThicknessMax = 8;
            QuadrantCentered = false;
            QuadrantIndices = new int[0];
            QuadrantGapDivisor = 8;
            UseCustomColor = false;
            UniformColor = ConsoleColor.White;
            CustomColors = new ConsoleColor[] { ConsoleColor.White, ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Blue };
            ReverseMode = false;
            ReverseVolumeSensitivity = 0.05f;
            SmoothMode = true;
            LerpFactor = 0.4f;
            Character = 'O';
            VerticalStack = true;
            CircleSegmentDensity = 0.8f;
            CircleMinSegments = 12;
            CircleMaxSegments = 120;
            SquareWidthRatio = 1.0f;
            SquareHeightRatio = 1.0f;
            TriangleSideMultiplier = 1.8f;
            TriangleHeightMultiplier = 0.87f;
            TriangleAspectCorrection = 0.45f;
            PyramidRowSpacing = 0.25f;
            PolygonSides = 5;
            FillMode = false;
            FillSpacing = 0;
        }

        public void EnforceConstraints()
        {
            if (VolumeSensitivity < 0.1f) VolumeSensitivity = 0.1f;
            if (VolumeSensitivity > 1f) VolumeSensitivity = 1f;

            if (MaxSizePercent < 0.02f) MaxSizePercent = 0.02f;
            if (MaxSizePercent > 0.99f) MaxSizePercent = 0.99f;
            if (MinSizePercent < 0f) MinSizePercent = 0f;
            if (MinSizePercent >= MaxSizePercent) MinSizePercent = MaxSizePercent - 0.01f;

            if (Count < 1) Count = 1;
            if (Count > 5) Count = 5;
            if (ConcentricLayers < 1) ConcentricLayers = 1;
            if (ConcentricLayers > 10) ConcentricLayers = 10;
            if (ConcentricPadding < 0) ConcentricPadding = 0;
            if (ThicknessMax < 1) ThicknessMax = 1;
            if (QuadrantGapDivisor < 5) QuadrantGapDivisor = 5;
            if (QuadrantGapDivisor > 20) QuadrantGapDivisor = 20;
            if (ReverseVolumeSensitivity < 0.01f) ReverseVolumeSensitivity = 0.01f;
            if (ReverseVolumeSensitivity > 0.05f) ReverseVolumeSensitivity = 0.05f;
            if (LerpFactor < 0.01f) LerpFactor = 0.01f;
            if (LerpFactor > 1f) LerpFactor = 1f;
            if (CircleSegmentDensity < 0.3f) CircleSegmentDensity = 0.3f;
            if (CircleSegmentDensity > 1.5f) CircleSegmentDensity = 1.5f;
            if (CircleMinSegments < 6) CircleMinSegments = 6;
            if (CircleMinSegments > 20) CircleMinSegments = 20;
            if (CircleMaxSegments < 60) CircleMaxSegments = 60;
            if (CircleMaxSegments > 200) CircleMaxSegments = 200;
            if (SquareWidthRatio < 0.1f) SquareWidthRatio = 0.1f;
            if (SquareWidthRatio > 5f) SquareWidthRatio = 5f;
            if (SquareHeightRatio < 0.1f) SquareHeightRatio = 0.1f;
            if (SquareHeightRatio > 5f) SquareHeightRatio = 5f;
            if (TriangleSideMultiplier < 0.5f) TriangleSideMultiplier = 0.5f;
            if (TriangleSideMultiplier > 4f) TriangleSideMultiplier = 4f;
            if (TriangleHeightMultiplier < 0.1f) TriangleHeightMultiplier = 0.1f;
            if (TriangleHeightMultiplier > 2f) TriangleHeightMultiplier = 2f;
            if (TriangleAspectCorrection < 0.1f) TriangleAspectCorrection = 0.1f;
            if (TriangleAspectCorrection > 1f) TriangleAspectCorrection = 1f;
            if (PyramidRowSpacing < 0.08f) PyramidRowSpacing = 0.08f;
            if (PyramidRowSpacing > 0.3f) PyramidRowSpacing = 0.3f;
            if (PolygonSides < 5) PolygonSides = 5;
            if (PolygonSides > 12) PolygonSides = 12;
            if (FillSpacing < 0) FillSpacing = 0;
            if (FillSpacing > 3) FillSpacing = 3;
        }

        public void EnforceMandatoryConstraints()
        {
            if (VolumeSensitivity < 0f) VolumeSensitivity = 0.01f;
            if (TriggerThreshold < 0f) TriggerThreshold = 0f;
            if (MaxSizePercent < 0f) MaxSizePercent = 0.01f;
            if (MinSizePercent < 0f) MinSizePercent = 0f;
            if (Count < 1) Count = 1;
            if (ConcentricLayers < 1) ConcentricLayers = 1;
            if (ConcentricPadding < 0) ConcentricPadding = 0;
            if (Thickness < 1) Thickness = 1;
            if (ThicknessMax < 1) ThicknessMax = 1;
            if (QuadrantGapDivisor < 1) QuadrantGapDivisor = 1;
            if (ReverseVolumeSensitivity < 0f) ReverseVolumeSensitivity = 0.01f;
            if (LerpFactor < 0f) LerpFactor = 0.01f;
            if (CircleSegmentDensity < 0f) CircleSegmentDensity = 0.01f;
            if (CircleMinSegments < 1) CircleMinSegments = 1;
            if (CircleMaxSegments < 1) CircleMaxSegments = 1;
            if (SquareWidthRatio < 0f) SquareWidthRatio = 0.01f;
            if (SquareHeightRatio < 0f) SquareHeightRatio = 0.01f;
            if (TriangleSideMultiplier < 0f) TriangleSideMultiplier = 0.01f;
            if (TriangleHeightMultiplier < 0f) TriangleHeightMultiplier = 0.01f;
            if (TriangleAspectCorrection < 0f) TriangleAspectCorrection = 0.01f;
            if (PyramidRowSpacing < 0f) PyramidRowSpacing = 0.01f;
            if (PolygonSides < 3) PolygonSides = 3;
            if (FillSpacing < 0) FillSpacing = 0;

            if (QuadrantIndices == null || QuadrantIndices.Length == 0)
                QuadrantIndices = new int[0];

            if (CustomColors == null || CustomColors.Length == 0)
                CustomColors = new ConsoleColor[] { ConsoleColor.White, ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Blue };

            if ((int)Type < 0 || (int)Type > Utility.EnumCount<ShapeType>(true))
                Type = ShapeType.Circle;

            if ((int)Layout < 0 || (int)Layout > Utility.EnumCount<ShapeLayout>(true))
                Layout = ShapeLayout.Single;

            if ((int)UniformColor < 0 || (int)UniformColor > Utility.EnumCount<ConsoleColor>(true))
                UniformColor = ConsoleColor.White;

            for (int i = 0; i < CustomColors.Length; i++)
                if ((int)CustomColors[i] < 0 || (int)CustomColors[i] > Utility.EnumCount<ConsoleColor>(true))
                    CustomColors[i] = ConsoleColor.White;

        }
    }
}

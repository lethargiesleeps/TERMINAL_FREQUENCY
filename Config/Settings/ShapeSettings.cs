using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Core;
using TERMINAL_FREQUENCY.Visualization.Shape;

namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class ShapeSettings : IConfigurable
    {
        public ShapeType Type { get; set; }
        public ShapeLayout Layout { get; set; }
        public float VolumeSensitivity { get; set; }
        public float TriggerThreshold { get; set; }
        public float MaxSizePercent { get; set; }
        public float MinSizePercent { get; set; }
        public int Count { get; set; }
        public int ConcentricLayers { get; set; }
        public int ConcentricPadding { get; set; }
        public int Thickness { get; set; }
        public int ThicknessMax { get; set; }
        public bool QuadrantCentered { get; set; }
        public int[] QuadrantIndices { get; set; }
        public int QuadrantGapDivisor { get; set; }
        public bool UseCustomColor { get; set; }
        public ConsoleColor UniformColor { get; set; }
        public ConsoleColor[] CustomColors { get; set; }
        public bool ReverseMode { get; set; }
        public float ReverseVolumeSensitivity { get; set; }
        public bool SmoothMode { get; set; }
        public float LerpFactor { get; set; }
        public char Character { get; set; }
        public bool VerticalStack { get; set; }
        public float CircleSegmentDensity { get; set; }
        public int CircleMinSegments { get; set; }
        public int CircleMaxSegments { get; set; }
        public float SquareWidthRatio { get; set; }
        public float SquareHeightRatio { get; set; }
        public float TriangleSideMultiplier { get; set; }
        public float TriangleHeightMultiplier { get; set; }
        public float TriangleAspectCorrection { get; set; }
        public float PyramidRowSpacing { get; set; }
        public int PolygonSides { get; set; }
        public bool FillMode { get; set; }
        public char[] FillCharacters { get; set; }
        public ConsoleColor[] FillColors { get; set; }
        public int FillSpacing { get; set; }

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
            Thickness = 1;
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
            Character = '█';
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
            FillCharacters = new char[] { '█', '▓', '▒', '█' };
            FillColors = new ConsoleColor[] { ConsoleColor.DarkGray, ConsoleColor.DarkGray, ConsoleColor.DarkGray, ConsoleColor.DarkGray };
            FillSpacing = 1;
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
            if (Count > 4) Count = 4;
            if (ConcentricLayers < 1) ConcentricLayers = 1;
            if (ConcentricLayers > 4) ConcentricLayers = 4;
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
            if (FillCharacters == null || FillCharacters.Length == 0)
                FillCharacters = new char[] { '█', '▓', '▒', '█' };
            if (FillColors == null || FillColors.Length == 0)
                FillColors = new ConsoleColor[] { ConsoleColor.DarkGray, ConsoleColor.DarkGray, ConsoleColor.DarkGray, ConsoleColor.DarkGray };
            if ((int)Type < 0 || (int)Type > Utility.EnumCount<ShapeType>(true))
                Type = ShapeType.Circle;
            if ((int)Layout < 0 || (int)Layout > Utility.EnumCount<ShapeLayout>(true))
                Layout = ShapeLayout.Single;
            if ((int)UniformColor < 0 || (int)UniformColor > Utility.EnumCount<ConsoleColor>(true))
                UniformColor = ConsoleColor.White;
            for (int i = 0; i < CustomColors.Length; i++)
                if ((int)CustomColors[i] < 0 || (int)CustomColors[i] > Utility.EnumCount<ConsoleColor>(true))
                    CustomColors[i] = ConsoleColor.White;
            for (int i = 0; i < FillColors.Length; i++)
                if ((int)FillColors[i] < 0 || (int)FillColors[i] > Utility.EnumCount<ConsoleColor>(true))
                    FillColors[i] = ConsoleColor.DarkGray;
        }
    }
}

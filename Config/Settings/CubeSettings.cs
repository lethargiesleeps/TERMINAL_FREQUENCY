using TERMINAL_FREQUENCY.Visualization.Cube;

namespace TERMINAL_FREQUENCY.Config.Settings
{
    public class CubeSettings : IConfigurable
    {
        public float CubeWidth { get; set; }
        public float DistanceFromCam { get; set; }
        public float ZoomLevel { get; set; }
        public float PointDensity { get; set; }
        public char[] FaceCharacters { get; set; }
        public CubeRotationMode RotationMode { get; set; }
        public RotationDirection Direction { get; set; }
        public ConsoleColor Color { get; set; }
        public float RotationSpeedX { get; set; }
        public float RotationSpeedY { get; set; }
        public float RotationSpeedZ { get; set; }
        public float ContinuousSpeedMultiplier { get; set; }
        public bool FreezeXRotation { get; set; }
        public bool FreezeYRotation { get; set; }
        public bool FreezeZRotation { get; set; }

        public float FrequencyThreshold { get; set; }
        public float VolumeThreshold { get; set; }
        public bool PulseEnabled { get; set; }
        public float PulseIntensity { get; set; }
        public float PulseDecay { get; set; }

        public CubeSettings()
        {
            Restore();
        }

        public void Restore()
        {
            CubeWidth = 20f;
            DistanceFromCam = 100f;
            ZoomLevel = 30f;
            PointDensity = 0.8f;
            FaceCharacters = new char[] { '@', '#', '%', '.', '=', '^' };
            RotationMode = CubeRotationMode.Continuous;
            Direction = RotationDirection.Forward;
            Color = ConsoleColor.White;
            RotationSpeedX = 0.1f;
            RotationSpeedY = 0.1f;
            RotationSpeedZ = 0.05f;
            FrequencyThreshold = 0.5f;
            VolumeThreshold = 0.3f;
            FreezeXRotation = false;
            FreezeYRotation = false;
            FreezeZRotation = false;
            PulseEnabled = true;
            PulseIntensity = 0.3f;
            PulseDecay = 0.95f;
            ContinuousSpeedMultiplier = 0.1f;
        }

        public void EnforceConstraints()
        {

            //if (cubewidth < 1f) cubewidth = 1f;
            //if (cubewidth > 50f) cubewidth = 50f;
            //if (distancefromcam < 10f) distancefromcam = 10f;
            //if (distancefromcam > 500f) distancefromcam = 500f;
            //if (zoomlevel < 1f) zoomlevel = 1f;
            //if (zoomlevel > 100f) zoomlevel = 100f;
            //if (pointdensity < 0.1f) pointdensity = 0.1f;
            //if (pointdensity > 2f) pointdensity = 2f;
            //if (rotationspeeda < 0f) rotationspeeda = 0f;
            //if (rotationspeeda > 1f) rotationspeeda = 1f;
            //if (rotationspeedb < 0f) rotationspeedb = 0f;
            //if (rotationspeedb > 1f) rotationspeedb = 1f;
            //if (rotationspeedc < 0f) rotationspeedc = 0f;
            //if (rotationspeedc > 1f) rotationspeedc = 1f;
            //if (frequencythreshold < 0f) frequencythreshold = 0f;
            //if (frequencythreshold > 1f) frequencythreshold = 1f;
            //if (volumethreshold < 0f) volumethreshold = 0f;
            //if (volumethreshold > 1f) volumethreshold = 1f;
            if (RotationMode == CubeRotationMode.Continuous)
            {
                RotationSpeedX = 0.01f;
                RotationSpeedY = 0.01f;
                RotationSpeedZ = 0.005f;
            }
        }

        public void EnforceMandatoryConstraints()
        {
            if (CubeWidth < 0f) CubeWidth = 0f;
            if (DistanceFromCam < 0f) DistanceFromCam = 0f;
            if (ZoomLevel < 0f) ZoomLevel = 0f;
            if (PointDensity <= 0f) PointDensity = 0.1f;
            if (RotationSpeedX < 0f) RotationSpeedX = 0f;
            if (RotationSpeedY < 0f) RotationSpeedY = 0f;
            if (RotationSpeedZ < 0f) RotationSpeedZ = 0f;
            if (FrequencyThreshold < 0f) FrequencyThreshold = 0f;
            if (VolumeThreshold < 0f) VolumeThreshold = 0f;
            if (FaceCharacters == null || FaceCharacters.Length == 0 || FaceCharacters.Length > 6)
                FaceCharacters = new char[] { '@', '#', '%', '.', '=', '^' };

        }
    }
}

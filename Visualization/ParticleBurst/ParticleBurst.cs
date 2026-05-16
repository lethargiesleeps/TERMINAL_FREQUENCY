using System.Xml.Linq;
using TERMINAL_FREQUENCY.Config.Settings;
using TERMINAL_FREQUENCY.Config.Settings.Visualizations;
using TERMINAL_FREQUENCY.Core.Rendering;

namespace TERMINAL_FREQUENCY.Visualization.ParticleBurst
{
    namespace TERMINAL_FREQUENCY.Visualization
    {
        public class ParticleBurst : ISpikeReactive
        {
            private Settings _settings;
            private string _name = "PARTICLES";
            private int _modeIndex = 6;
            private List<Particle> _particles = new List<Particle>();
            private int _lastCenterX = 60;
            private int _lastCenterY = 20;
            private Random _rnd = new Random();

            string IVisualization.Name => _name;
            int IVisualization.ModeIndex => _modeIndex;

            public ParticleBurst(Settings settings)
            {
                _settings = settings;
            }

            public void Draw(ScreenBuffer buffer)
            {
                ParticleBurstSettings pb = _settings.ParticleBurst;
                _lastCenterX = buffer.Width / 2;
                _lastCenterY = buffer.Height / 2;

                for (int i = _particles.Count - 1; i >= 0; i--)
                {
                    Particle particle = _particles[i];
                    particle.X += particle.Vx;
                    particle.Y += particle.Vy;
                    particle.Life -= pb.FadeRate;

                    if (particle.Life <= 0)
                    {
                        _particles.RemoveAt(i);
                        continue;
                    }

                    int sx = (int)particle.X;
                    int sy = (int)particle.Y;

                    if (sx < 0 || sx >= buffer.Width || sy < 0 || sy >= buffer.Height)
                        continue;

                    float lifeRatio = particle.Life / particle.MaxLife;
                    ConsoleColor color = pb.Color;
                    if (lifeRatio < 0.3f) color = ConsoleColor.DarkGray;
                    else if (lifeRatio < 0.6f) color = ConsoleColor.Gray;

                    buffer.SetPixel(sx, sy, particle.Character, color);
                }
            }

            public void OnSpike() => OnSpike(1f);

            public void OnSpike(float intensity)
            {
                var pb = _settings.ParticleBurst;

                for (int b = 0; b < pb.BurstsPerSpike; b++)
                {
                    int centerX = _rnd.Next(_lastCenterX * 2);
                    int centerY = _rnd.Next(_lastCenterY * 2);

                    for (int i = 0; i < pb.ParticleCount; i++)
                    {
                        float angle = (float)(_rnd.NextDouble() * pb.SpreadAngle * Math.PI / 180.0);
                        float speed = pb.SpeedMin + (float)_rnd.NextDouble() * (pb.SpeedMax - pb.SpeedMin);
                        float life = pb.LifeMin + (float)_rnd.NextDouble() * (pb.LifeMax - pb.LifeMin);

                        _particles.Add(new Particle
                        {
                            X = centerX,
                            Y = centerY,
                            Vx = MathF.Cos(angle) * speed,
                            Vy = MathF.Sin(angle) * speed * 0.45f,
                            Life = life,
                            MaxLife = life,
                            Character = pb.CharacterSet[_rnd.Next(pb.CharacterSet.Length)]
                        });
                    }
                }
            }
        }
    }
}

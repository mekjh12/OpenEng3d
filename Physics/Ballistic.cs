using OpenGL;
using Shader;
using System.Collections.Generic;
using ZetaExt;

namespace Physics
{
    public class Ballistic
    {
        /// <summary>
        /// 슛팅 종류
        /// </summary>
        public enum ShotType
        {
            UNUSED = 0,
            PISTOL,
            ARTILLERY,
            LASER,
            FIREBALL,
        };

        List<Particle> _particles;

        public Ballistic()
        {
            _particles = new List<Particle>(); 
        }

        public void AddParticle(ShotType shotType, Vertex3f position, Vertex3f direction)
        {
            Particle particle = CreateParticle(shotType, position, direction);
            _particles.Add(particle);
        }

        private Particle CreateParticle(ShotType shotType, Vertex3f position, Vertex3f direction)
        {
            Particle particle = new Particle();
            particle.ClearAccumulator();
            particle.Position = position;
            switch (shotType)
            {
                case ShotType.UNUSED:
                    break;
                case ShotType.PISTOL:
                    particle.Mass = 2.0f;
                    particle.Velocity = direction * 35.0f; // 35m/s
                    particle.Acceleration = -Vertex3f.UnitZ;
                    particle.Damping = 0.99f;
                    break;
                case ShotType.ARTILLERY:
                    particle.Mass = 200.0f;
                    particle.Velocity = direction * 50.0f;
                    particle.Acceleration = -Vertex3f.UnitZ * 20.0f;
                    particle.Damping = 0.99f;
                    break;
                case ShotType.FIREBALL:
                    particle.Mass = 1.0f;
                    particle.Velocity = direction * 10.0f;
                    particle.Acceleration = Vertex3f.UnitZ * 0.6f;
                    particle.Damping = 0.9f;
                    break;
                case ShotType.LASER:
                    particle.Mass = 0.1f;
                    particle.Velocity = direction * 100.0f;
                    particle.Acceleration = Vertex3f.Zero;
                    particle.Damping = 0.99f;
                    break;
                default:
                    break;
            }

            return particle;
        }


        public void Update(float duration)
        {
            List<Particle> removeParticle = new List<Particle>();

            foreach (Particle particle in _particles)
            {
                particle.Integrate(duration);
                particle.Life -= duration;
                if (particle.Life < 0.0f)
                {
                    removeParticle.Add(particle);
                }
                //Console.WriteLine($"{particle.Guid}={particle.Position}");
            }
            //Console.WriteLine("--------------------------------");

            // 10.0f이후이 파티클은 제거한다.
            foreach (Particle particle in removeParticle)
            {
                _particles.Remove(particle);
            }
        }

        public void Render(uint vao, int vertexCount, ColorShader shader, Matrix4x4f proj, Matrix4x4f view)
        {
            shader.Bind();

            Matrix4x4f vp = proj * view;

            Debug.Write(_particles.Count + "개");

            foreach (Particle particle in _particles)
            {
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, particle.Color);
                Matrix4x4f model = Matrix4x4f.Identity;
                model.Scale(0.2f, 0.2f, 0.2f);
                model = model.Column(3, particle.Position);
                shader.LoadUniform(ColorShader.UNIFORM_NAME.mvp, vp * model);
                Gl.BindVertexArray(vao);
                Gl.EnableVertexAttribArray(0);
                Gl.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);
                Gl.DisableVertexAttribArray(0);
                Gl.BindVertexArray(0);
            }
            shader.Unbind();
        }
    }
}

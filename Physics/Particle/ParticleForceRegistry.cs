using OpenGL;
using Shader;
using System;
using ZetaExt;

namespace Physics
{
    public class ParticleForceRegistry
    {
        const int MAX_PARTICLE_COUNT = 1024;
        const int MAX_PARTICLE_FORCE_COUNT = 1024 * 3;
        int _nextParticle = 0;
        int _nextParticleForce = 0;
        Particle[] _particles;
        ParticleForceRegistration[] _particleForceRegistrations;
        int _particleForceCount = 0;
        int _particleCount = 0;

        public int ParticleForceCount => _particleForceCount;

        public int ParticleCount => _particleCount;
        
        /// <summary>
        /// 생성자
        /// </summary>
        public ParticleForceRegistry()
        {
            _particles = new Particle[MAX_PARTICLE_COUNT];
            _particleForceRegistrations = new ParticleForceRegistration[MAX_PARTICLE_FORCE_COUNT];
        }

        /// <summary>
        /// 입자를 추가한다.
        /// </summary>
        /// <param name="particle"></param>
        public void AddParticle(Particle particle)
        {
            particle.IsRegisty = true;

            // 입자를 지정할 위치가 비워있지 않으면 이전에 위치한 입자를 제거한다.
            if (_particles[_nextParticle] != null)
            {
                RemoveParticle(_nextParticle);
            }

            // 입자를 새로운 위치에 지정한다.
            _particles[_nextParticle] = particle;
            _particleCount++;
            _nextParticle = (_nextParticle + 1) % MAX_PARTICLE_COUNT;
        }

        /// <summary>
        /// 입자에 힘을 준다.
        /// </summary>
        /// <param name="particle"></param>
        /// <param name="fg"></param>
        public void AddForce(Particle particle, IParticleForceGenerator fg)
        {
            // 입자가 등록되어 있는지 검사한다.
            if (!particle.IsRegisty)
            {
                throw new System.Exception("입자에 힘을 주기 위해서는 입자를 먼저 등록해야 합니다.");
            }

            // 입자와 힘의 순서쌍을 만들어 등록한다.
            ParticleForceRegistration particleForceRegistration = new ParticleForceRegistration();
            particleForceRegistration.Particle = particle;
            particleForceRegistration.ForceGenerator = fg;
            _particleForceRegistrations[_nextParticleForce] = particleForceRegistration;            
            particle.AddForceRegistration(_nextParticleForce); // 삭제를 위한 힘등록기를 입자가 가지고 있는다.

            _nextParticleForce = (_nextParticleForce + 1) % MAX_PARTICLE_FORCE_COUNT;
        }

        /// <summary>
        /// 입자를 제거한다.
        /// </summary>
        /// <param name="index"></param>
        protected void RemoveParticle(int index)
        {
            Particle particle = _particles[index];

            // 힘등록기에서 입자에 해당하는 힘을 모두 제거한다.
            foreach (int forceRegistration in _particles[index].Forces)
            {
                _particleForceRegistrations[forceRegistration] = null;
            }

            // 입자를 제거한다.
            _particles[index] = null;
        }

        public void Clear()
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i] = null;
            }

            for (int i = 0; i < _particleForceRegistrations.Length; i++)
            {
                _particleForceRegistrations[i] = null;
            }

            _particleCount = 0;
            _nextParticleForce = 0;
        }

        /// <summary>
        /// 힘을 업데이트한다.
        /// </summary>
        /// <param name="duration"></param>
        public void UpdateForces(float duration)
        {
            _particleForceCount = 0;

            // 입자에 힘을 준다.
            foreach (ParticleForceRegistration item in _particleForceRegistrations)
            {
                if (item == null) continue;
                Particle particle = item.Particle;
                IParticleForceGenerator fg = item.ForceGenerator;
                fg.UpdateForce(particle, duration);
                _particleForceCount++;
            }
        }

        /// <summary>
        /// 업데이트를 실행한다.
        /// </summary>
        /// <param name="duration"></param>
        public void Update(float duration)
        {
            _particleCount = 0;

            for (int i = 0; i < _particles.Length; i++)
            {
                Particle particle = _particles[i];
                if (particle == null) continue;
                particle.Integrate(duration);
                particle.Life -= duration;
                _particleCount++;

                // 입자의 수명이 다하면 제거한다.
                if (particle.Life < 0)
                {
                    RemoveParticle(i);
                }
            }
        }

        public void Render(uint vao, int vertexCount, ColorShader shader, Matrix4x4f proj, Matrix4x4f view)
        {
            shader.Bind();
            Matrix4x4f vp = proj * view;

            foreach (Particle particle in _particles)
            {
                if (particle == null) continue;
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, particle.Color);
                Matrix4x4f model = Matrix4x4f.Identity;
                model.Scale(particle.HalfSize, particle.HalfSize, particle.HalfSize);
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

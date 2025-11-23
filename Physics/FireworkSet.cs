using OpenGL;
using Shader;
using ZetaExt;
using static Physics.Firework;
using static Physics.Firework.FireWorkRule;

namespace Physics
{
    public class FireworkSet
    {
        const int MAx_FIREWORKS = 1024;
        const int RULE_COUNT = 9;

        Firework[] _fireworkList;
        FireWorkRule[] _fireWorkRules;
        int _nextFirework;

        public FireworkSet()
        {
            _fireworkList = new Firework[MAx_FIREWORKS];
            _fireWorkRules = new FireWorkRule[RULE_COUNT];
        }

        public void InitFireworkRules()
        {
            RegistryFirework(0, type: 1, 0.5f, 1.4f, new Vertex3f(-5, -5, 5), new Vertex3f(5, 5, 9), 0.1f, new Vertex2i[2] { new Vertex2i(3, 5), new Vertex2i(5, 5) });
            RegistryFirework(1, type: 2, 0.5f, 2.0f, new Vertex3f(-5, -5, 10), new Vertex3f(5, 5, 20), 0.8f, new Vertex2i[1] { new Vertex2i(4, 10) });
            RegistryFirework(2, type: 3, 0.5f, 1.5f, new Vertex3f(-5, -5, -5), new Vertex3f(5, 5, 5), 0.1f, null);
            RegistryFirework(3, type: 4, 0.25f, 0.5f, new Vertex3f(-20, -5, 5), new Vertex3f(20, 5, 5), 0.2f, null);
            RegistryFirework(4, type: 5, 0.5f, 1.0f, new Vertex3f(-20, -5, 2), new Vertex3f(20, 5, 18), 0.01f, new Vertex2i[1] { new Vertex2i(3, 5) });
            RegistryFirework(5, type: 6, 3.0f, 5.0f, new Vertex3f(-5, -5, 5), new Vertex3f(5, 5, 10), 0.95f, null);
            RegistryFirework(6, type: 7, 4.0f, 5.0f, new Vertex3f(-5, -5, 50), new Vertex3f(5, 5, 60), 0.01f, new Vertex2i[1] { new Vertex2i(8, 10)});
            RegistryFirework(7, type: 8, 0.25f, 0.5f, new Vertex3f(-1, -1, -1), new Vertex3f(1, 1, 1), 0.01f, null);
            RegistryFirework(8, type: 9, 3.0f, 5.0f, new Vertex3f(-15, -5, 10), new Vertex3f(15, 5, 15), 0.95f, null);

            void RegistryFirework(int index, uint type, float minAge, float maxAge, Vertex3f minVelocity, Vertex3f maxVelocity, float damping, Vertex2i[] payloads)
            {
                FireWorkRule fireWorkRule = new FireWorkRule();
                fireWorkRule.SetParameters(type, minAge, maxAge, minVelocity, maxVelocity, damping);
                if (payloads != null)
                {
                    fireWorkRule.Init((uint)payloads.Length);
                    for (int i = 0; i < payloads.Length; i++)
                    {
                        fireWorkRule.Payloads[i].Set(type: (uint)payloads[i].x, count: (uint)payloads[i].y);
                    }
                }
                _fireWorkRules[index] = fireWorkRule;
            }
        }

        public void Create(uint type, Vertex3f start, Firework parent)
{
            FireWorkRule rule = _fireWorkRules[(int)type - 1];
            if (_fireworkList[_nextFirework] == null)
            {
                _fireworkList[_nextFirework] = new Firework();
            }
            rule.Create(_fireworkList[_nextFirework], start, parent);
            _nextFirework = (_nextFirework + 1) % MAx_FIREWORKS;
        }

        public void Create(uint type, int number, Vertex3f start, Firework parent)
        {
            for (int i = 0; i < number; i++)
            {
                Create(type, start, parent);
            }
        }



        public void Update(float duration)
        {
            if (duration <= 0.0f) return;

            if (_fireworkList == null) return;

            foreach (Firework firework in _fireworkList)
            {
                if (firework == null) continue;

                // Check if we need to process this firework.
                if (firework.Type > 0)
                {
                    // Does it need removing?
                    if (firework.Update(duration))
                    {
                        // Find the appropriate rule
                        FireWorkRule rule = _fireWorkRules[firework.Type - 1];
                        firework.Type = 0;

                        // Add the payload
                        for (uint i = 0; i < rule.PayloadCount; i++)
                        {
                            PayLoad payload = rule.Payloads[i];
                            Create(payload.Type, (int)payload.Count, Vertex3f.Zero, firework);
                        }
                    }
                }
            }
        }

        public void Render(uint vao, int vertexCount, ColorShader shader, Matrix4x4f proj, Matrix4x4f view)
        {
            shader.Bind();

            Matrix4x4f vp = proj * view;

            foreach (Firework firework in _fireworkList)
            {
                if (firework == null) continue;
                if (firework.Type == 0) continue;
                shader.LoadUniform(ColorShader.UNIFORM_NAME.color, firework.Color);
                Matrix4x4f model = Matrix4x4f.Identity;
                model.Scale(0.05f, 0.05f, 0.05f);
                model = model.Column(3, firework.Position);
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

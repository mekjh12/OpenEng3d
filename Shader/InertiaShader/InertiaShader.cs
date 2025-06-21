using OpenGL;
using System.Drawing.Drawing2D;
using System.IO;
using Common;

namespace Shader
{
    public class InertiaShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        const string vertex_sources = @"
        #version 420 core

        in vec3 position;
        out vec4 worldPosition;
        out vec3 center;

        uniform mat4 model;
        uniform mat4 proj;
        uniform mat4 view;

        void main(void)
        {
            center = model[3].xyz;
            worldPosition = model * vec4(position, 1.0);
            gl_Position = proj * view * worldPosition;
        }
        ";

        const string fragment_sources = @"
        #version 420 core

        uniform mat3 inverseInertia;
        uniform vec3 axis;
        in vec3 center;
        in vec4 worldPosition;
        out vec4 FragColor;

        void main(void)
        {
            vec3 relPoint = normalize(worldPosition.xyz - center);
            vec3 torque = inverseInertia * relPoint;
            float dot = dot(relPoint, torque);
            FragColor = vec4(dot, 0, 0, 1.0f);
        }
        ";

        public InertiaShader(string projectPath) : base()
        {
            _name = this.GetType().Name;

            string vertFileName = projectPath + "\\vert.tmp";
            File.WriteAllText(vertFileName, vertex_sources);
            VertFileName = vertFileName;

            string fragFileName = projectPath + "\\frag.tmp";
            File.WriteAllText(fragFileName, fragment_sources);
            FragFilename = fragFileName;

            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

        protected override void GetAllUniformLocations()
        {
            UniformLocations("model", "view", "proj");
            UniformLocations("inverseInertia", "axis");
        }

        public void LoadTexture(string textureUniformName, TextureUnit textureUnit, uint texture)
        {
            base.LoadInt(_location[textureUniformName], textureUnit - TextureUnit.Texture0);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadRotationAxis(Vertex3f axis)
        { 
            base.LoadVector(_location["axis"], axis);
        }

        public void LoadProjMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["proj"], matrix);
        }

        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["view"], matrix);
        }

        public void LoadModelMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["model"], matrix);
        }

        public void LoadInverseInertia(Matrix3x3f matrix)
        {
            base.LoadMatrix(_location["inverseInertia"], matrix);
        }

    }
}

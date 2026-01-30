using Common;
using OpenGL;

namespace Shader
{
    public class WaterDebugShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\water_debug.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\water_debug.geom";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\water_debug.frag";

        private int loc_waterBuffer;

        public WaterDebugShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_waterBuffer = GetUniformLocation("u_WaterBuffer");
        }

        protected override void BindAttributes()
        {
        }

        public void LoadWaterBuffer(TextureUnit unit, uint textureId)
        {
            // TextureUnit을 숫자로 변환 (Texture0 = 0, Texture1 = 1, ...)
            int textureUnitNumber = (int)unit - (int)TextureUnit.Texture0;
            Gl.Uniform1(loc_waterBuffer, textureUnitNumber);  // ✅ 숫자 전달
            Gl.ActiveTexture(unit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }
    }
}
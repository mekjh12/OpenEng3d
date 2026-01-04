using Common;
using Common.Abstractions;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 월드 축(World Axis) 렌더링 셰이더
    /// Geometry Shader를 사용해 Point를 X, Y, Z 축 라인으로 확장합니다.
    /// </summary>
    public class WorldAxisShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\world_axis.vert";
        const string GEOMETRY_FILE = @"\Shader\GPUDriven\glsl\world_axis.geom.glsl";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\world_axis.frag";

        // 유니폼 위치
        private int loc_vp;
        private int loc_axisLength;

        public WorldAxisShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_vp = GetUniformLocation("vp");
            loc_axisLength = GetUniformLocation("axisLength");
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
        }

        public void LoadVPMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_vp, matrix);
        }

        public void LoadAxisLength(float length)
        {
            Gl.Uniform1(loc_axisLength, length);
        }
    }
}
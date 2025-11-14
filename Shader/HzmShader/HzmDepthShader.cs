using OpenGL;
using System.Drawing.Drawing2D;
using System.IO;
using Common;

namespace Shader
{
    /// <summary>
    /// 계층적 Z버퍼를 화면 평면에 그려주는 세이더
    /// </summary>
    public class HzmDepthShader : ShaderProgram<HzmDepthShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            LOD,
            /// <summary>
            /// 깊이맵을 원근형 렌더링 여부
            /// </summary>
            IsPerspective,
            DepthTexture,
            CameraFar,
            CameraNear,
            Count
        }

        const string VERTEX_FILE = @"\Shader\HzmShader\dummy.vert";
        const string GEOMETRY_FILE = @"\Shader\HzmShader\post.gs.glsl";
        const string FRAGMENT_FILE = @"\Shader\HzmShader\depth.frag";

        public HzmDepthShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }

        protected override void BindAttributes()
        {
            //base.BindAttribute(0, "position");
        }

    }
}

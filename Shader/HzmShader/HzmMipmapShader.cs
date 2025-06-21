using OpenGL;
using Common;

namespace Shader
{
    /// <summary>
    /// 계층적 깊이버퍼을 렌더링하기 위한 레벨에 따른 2x2 최솟값 쉐이더
    /// </summary>
    public class HzmMipmapShader : ShaderProgram<HzmMipmapShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            DepthBuffer,
            LastMipSize,
            Count
        }

        const string VERTEX_FILE = @"\Shader\HzmShader\dummy.vert";
        const string GEOMETRY_FILE = @"\Shader\HzmShader\hi-z.gs.glsl";
        const string FRAGMENT_FILE = @"\Shader\HzmShader\hi-z.frag";

        public HzmMipmapShader(string projectPath) : base()
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
                string uniformname = ((UNIFORM_NAME)i).ToString();
                UniformLocation(uniformname);
            }
        }

        protected override void BindAttributes()
        {
            //base.BindAttribute(0, "position");
        }

    }
}

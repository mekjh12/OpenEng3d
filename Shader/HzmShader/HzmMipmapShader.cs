using OpenGL;
using Common;
using Common.Abstractions;

namespace Shader
{
    /// <summary>
    /// 계층적 깊이버퍼을 렌더링하기 위한 레벨에 따른 2x2 최솟값 쉐이더
    /// </summary>
    public class HzmMipmapShader : ShaderProgramBase
    {
        int loc_DepthBuffer;
        int loc_LastMipSize;

        const string VERTEX_FILE = @"\Shader\HzmShader\dummy.vert";
        const string GEOMETRY_FILE = @"\Shader\HzmShader\hi-z.gs.glsl";
        const string FRAGMENT_FILE = @"\Shader\HzmShader\hi-z.frag";

        public HzmMipmapShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_DepthBuffer = GetUniformLocation("DepthBuffer");
            loc_LastMipSize = GetUniformLocation("LastMipSize");
        }

        protected override void BindAttributes()
        {
            //base.BindAttribute(0, "position");
        }

        public void LoadDepthBuffer(TextureUnit textureUnit, uint texture)
        {
            int ind = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_DepthBuffer, ind);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        public void LoadLastMipSize(Vertex2i size)
        {
            Gl.Uniform2(loc_LastMipSize, size.x, size.y);
        }


    }
}

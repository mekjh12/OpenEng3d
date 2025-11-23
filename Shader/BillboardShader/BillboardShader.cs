using OpenGL;
using Common;

namespace Shader
{
    /// <summary>
    /// 쉐이더를 사용하기 위해서는 실행 위치의 \Shader\Terrain\에 소스코드를 넣어주세요.
    /// </summary>
    public class BillboardShader : ShaderProgram<UnlitShader.UNIFORM_NAME>
    {
        const string VERTEx_FILE = @"\Shader\BillboardShader\billboard.vert";
        const string FRAGMENT_FILE = @"\Shader\BillboardShader\billboard.frag";
        const string GEOMETRY_FILE = @"\Shader\BillboardShader\billboard.gem.glsl";

        public BillboardShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEx_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            InitCompileShader();
        }

        public void LoadTexture(uint texture)
        {
            base.LoadInt(_location["gColorMap"], (int)TextureUnit.Texture0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
        }

        protected override void GetAllUniformLocations()
        {
            UniformLocations("proj", "view", "gCameraPos", "gColorMap");
            UniformLocations("fogColor", "fogDensity", "fogPlane");
            UniformLocations("atlasIndex");
        }

        public void LoadCameraPosition(Vertex3f pos) => base.LoadVector(_location["gCameraPos"], pos);

        public void LoadViewProjMatrix(Matrix4x4f matrix) => base.LoadMatrix(_location["gVP"], matrix);
        
        public void LoadProjMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["proj"], matrix);
        }

        public void LoadViewMatrix(Matrix4x4f matrix)
        {
            base.LoadMatrix(_location["view"], matrix);
        }

        public void LoadFogPlane(Vertex4f fogPlane)
        {
            base.LoadVector(_location["fogPlane"], fogPlane);
        }

        public void LoadFogDensity(float density)
        {
            base.LoadFloat(_location["fogDensity"], density);
        }

        public void LoadFogColor(Vertex3f fogcolor)
        {
            base.LoadVector(_location["fogColor"], fogcolor);
        }

    }
}

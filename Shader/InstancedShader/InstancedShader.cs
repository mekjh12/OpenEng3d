using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 인스턴싱 렌더링 셰이더
    /// GPU 인스턴싱을 사용하여 동일한 모델을 여러 번 렌더링합니다.
    /// </summary>
    public class InstancedShader : ShaderProgramBase
    {
        const string VERTEx_FILE = @"\Shader\InstancedShader\instanced.vert";
        const string FRAGMENT_FILE = @"\Shader\InstancedShader\instanced.frag";

        // 유니폼 위치 캐싱
        private int loc_vp;
        private int loc_modelTexture;

        public InstancedShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEx_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;

            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_vp = GetUniformLocation("vp");
            loc_modelTexture = GetUniformLocation("modelTexture");
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "texcoord");
            // location 2,3,4,5는 인스턴스 모델 행렬 (mat4)
        }

        /// <summary>
        /// View-Projection 행렬을 설정합니다.
        /// </summary>
        public void LoadVPMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_vp, matrix);
        }

        /// <summary>
        /// 텍스처를 설정합니다.
        /// </summary>
        public void LoadTexture(TextureUnit textureUnit, uint textureId)
        {
            int ind = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_modelTexture, ind);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }
    }
}
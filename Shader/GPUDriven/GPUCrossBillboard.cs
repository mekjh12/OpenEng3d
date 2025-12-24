using Common;
using OpenGL;
using System.IO;

namespace Shader
{
    public class GPUCrossBillboard : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\GPUDriven\glsl\billboard.vert";
        const string FRAGMENT_FILE = @"\Shader\GPUDriven\glsl\billboard.frag";

        // 유니폼 위치
        private int loc_vp;
        private int loc_cameraPos;
        private int loc_cameraRight;
        private int loc_cameraUp;
        private int loc_texture;
        private int loc_billboardSize;  // 추가!

        public GPUCrossBillboard(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;

            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_vp = GetUniformLocation("vp");
            loc_cameraPos = GetUniformLocation("cameraPos");
            loc_cameraRight = GetUniformLocation("cameraRight");
            loc_cameraUp = GetUniformLocation("cameraUp");
            loc_texture = GetUniformLocation("uTexture");
            loc_billboardSize = GetUniformLocation("uBillboardSize");  // 추가!
        }

        protected override void BindAttributes()
        {
            BindAttribute(0, "aPosition");
            BindAttribute(1, "aTexCoord");
        }

        public void LoadVPMatrix(Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_vp, matrix);
        }

        public void LoadCameraVectors(Vertex3f position, Vertex3f right, Vertex3f up)
        {
            Gl.Uniform3f(loc_cameraPos, 1, position);
            Gl.Uniform3f(loc_cameraRight, 1, right);
            Gl.Uniform3f(loc_cameraUp, 1, up);
        }

        /// <summary>
        /// 빌보드 크기를 설정합니다 (고정 크기 사용)
        /// </summary>
        public void LoadBillboardSize(Vertex2f size)
        {
            Gl.Uniform2f(loc_billboardSize, 1, size);
        }

        /// <summary>
        /// 텍스처 유닛을 설정합니다.
        /// </summary>
        public void LoadTexture(TextureUnit unit, uint textureId)
        {
            Gl.Uniform1(loc_texture, (uint)unit);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
        }
    }
}
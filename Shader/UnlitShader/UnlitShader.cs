using Common;
using OpenGL;
using ZetaExt;

namespace Shader
{
    /// <summary>
    /// 기본적인 Unlit 셰이더를 구현한 클래스입니다.
    /// 텍스처와 MVP 변환을 지원하며 조명 계산은 수행하지 않습니다.
    /// </summary>
    public class UnlitShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\UnlitShader\unlit.vert";
        const string FRAGMENT_FILE = @"\Shader\UnlitShader\unlit.frag";

        // 유니폼 위치 (캐싱)
        private int loc_mvp;
        private int loc_modelTexture;
        private int loc_textureCount;
        private int[] loc_textures;
        private const int MAX_TEXTURES = 32;
        private int loc_modelView;
        private int loc_normalMatrix;
        private int loc_enableLighting;

        public UnlitShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_mvp = GetUniformLocation("mvp");
            loc_modelView = GetUniformLocation("mv");
            loc_textureCount = GetUniformLocation("textureCount");
            loc_normalMatrix = GetUniformLocation("normalMatrix");
            loc_enableLighting = GetUniformLocation("enableLighting");

            loc_textures = new int[MAX_TEXTURES];
            for (int i = 0; i < MAX_TEXTURES; i++)
            {
                loc_textures[i] = GetUniformLocation($"textures[{i}]");
            }
        }

        public void LoadEnableLighting(bool enable)
        {
            Gl.Uniform1(loc_enableLighting, enable ? 1 : 0);
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "textureCoords");
            base.BindAttribute(2, "normal");
            base.BindAttribute(3, "materialID");
        }

        // ✅ 개선: 모든 변환 행렬을 한 번에 설정
        public void LoadTransforms(Matrix4x4f mvp, Matrix4x4f mv, Matrix4x4f model)
        {
            LoadUniformMatrix4(loc_mvp, mvp);
            LoadUniformMatrix4(loc_modelView, mv);
            LoadUniformMatrix3(loc_normalMatrix, CalculateNormalMatrix(model));
        }

        private Matrix3x3f CalculateNormalMatrix(Matrix4x4f modelView)
        {
            // 1. ModelView의 3x3 회전 부분 추출
            Matrix3x3f mv3x3 = modelView.Rot3x3f();

            // 2. 역행렬 계산
            Matrix3x3f inverse = mv3x3.Inverse;

            // 3. 전치
            Matrix3x3f normalMatrix = inverse.Transposed;

            return normalMatrix;
        }

        // === 레거시 메서드들 (하위 호환성) ===

        public void LoadNormalMatrix(Matrix3x3f normalMatrix)
        {
            Gl.UniformMatrix3f(loc_normalMatrix, 1, false, normalMatrix);
        }

        public void LoadModelView(Matrix4x4f modelView)
        {
            Gl.UniformMatrix4f(loc_modelView, 1, false, modelView);
        }

        public void LoadMVPMatrix(Matrix4x4f matrix)
        {
            Gl.UniformMatrix4f(loc_mvp, 1, false, matrix);
        }

        public void LoadTextureArray(uint[] textureIDs)
        {
            int count = System.Math.Min(textureIDs.Length, MAX_TEXTURES);

            Gl.Uniform1(loc_textureCount, count);

            for (int i = 0; i < count; i++)
            {
                Gl.ActiveTexture(TextureUnit.Texture0 + i);
                Gl.BindTexture(TextureTarget.Texture2d, textureIDs[i]);
                Gl.Uniform1(loc_textures[i], i);
            }
        }

        public void LoadModelTexture(TextureUnit textureUnit, uint texture)
        {
            Gl.Uniform1(loc_modelTexture, (int)TextureUnit.Texture0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }
    }
}
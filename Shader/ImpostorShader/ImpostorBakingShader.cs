using Common;
using OpenGL;
using ZetaExt;

namespace Shader
{
    /// <summary>
    /// 임포스터 아틀라스 베이킹 전용 셰이더
    /// MRT(Multiple Render Targets)로 Albedo, Normal, Depth를 동시 출력
    /// </summary>
    public class ImpostorBakingShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\ImpostorShader\impostor_baking.vert";
        const string FRAGMENT_FILE = @"\Shader\ImpostorShader\impostor_baking.frag";

        // 유니폼 위치 캐싱
        private int loc_mvp;
        private int loc_mv;
        private int loc_normalMatrix;
        private int loc_textureCount;
        private int[] loc_textures;
        private const int MAX_TEXTURES = 32;

        public ImpostorBakingShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            InitCompileShader();
        }

        protected override void GetAllUniformLocations()
        {
            loc_mvp = GetUniformLocation("mvp");
            loc_mv = GetUniformLocation("mv");
            loc_normalMatrix = GetUniformLocation("normalMatrix");
            loc_textureCount = GetUniformLocation("textureCount");

            // 텍스처 배열 유니폼
            loc_textures = new int[MAX_TEXTURES];
            for (int i = 0; i < MAX_TEXTURES; i++)
            {
                loc_textures[i] = GetUniformLocation($"textures[{i}]");
            }
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "textureCoords");
            base.BindAttribute(2, "normal");
            base.BindAttribute(3, "materialID");
        }

        /// <summary>
        /// 변환 행렬 일괄 설정
        /// </summary>
        public void LoadTransforms(Matrix4x4f mvp, Matrix4x4f mv, Matrix4x4f model)
        {
            LoadUniformMatrix4(loc_mvp, mvp);
            LoadUniformMatrix4(loc_mv, mv);
            LoadUniformMatrix3(loc_normalMatrix, CalculateNormalMatrix(model));
        }

        /// <summary>
        /// Normal Matrix 계산 (Model의 역전치 행렬)
        /// </summary>
        private Matrix3x3f CalculateNormalMatrix(Matrix4x4f model)
        {
            // 1. Model의 3x3 회전 부분 추출
            Matrix3x3f model3x3 = model.Rot3x3f();

            // 2. 역행렬 계산
            Matrix3x3f inverse = model3x3.Inverse;

            // 3. 전치
            Matrix3x3f normalMatrix = inverse.Transposed;

            return normalMatrix;
        }

        /// <summary>
        /// 텍스처 배열 바인딩 및 유니폼 설정
        /// </summary>
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
    }
}
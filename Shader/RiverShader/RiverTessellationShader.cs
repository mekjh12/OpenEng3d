using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 강 렌더링을 위한 테셀레이션 셰이더입니다. (Zero-allocation)
    /// 강 마스크 기반으로 AABB 쿼드 패치를 테셀레이션하며,
    /// TCS에서 프러스텀 컬링 + 강 마스크 컬링으로 불필요한 패치를 조기 폐기합니다.
    /// 
    /// 텍스처 유닛 할당:
    /// - Unit 0: 고해상도 높이맵 (heightMapHighRes)
    /// - Unit 1: 저해상도 높이맵 (heightMapLowRes)
    /// - Unit 2: 강 마스크맵 (riverMask)
    /// </summary>
    public class RiverTessellationShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\RiverShader\river.vert";
        const string TCS_FILE = @"\Shader\RiverShader\river.tcs.glsl";
        const string TES_FILE = @"\Shader\RiverShader\river.tes.glsl";
        const string FRAGMENT_FILE = @"\Shader\RiverShader\river.frag";

        // --- 변환 행렬 ---
        private int loc_model;

        // --- 높이맵 ---
        private int loc_heightMapHighRes;
        private int loc_heightMapLowRes;
        private int loc_blendFactor;
        private int loc_heightScale;

        // --- 강 마스크 ---
        private int loc_riverMask;

        // --- 강 파라미터 ---
        private int loc_riverHeightOffset;

        public RiverTessellationShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            TcsFileName = projectPath + TCS_FILE;
            TesFileName = projectPath + TES_FILE;

            InitCompileShader();
        }

        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");
            base.BindAttribute(1, "texCoord");
        }

        protected override void GetAllUniformLocations()
        {
            loc_model = GetUniformLocation("model");

            loc_heightMapHighRes = GetUniformLocation("heightMapHighRes");
            loc_heightMapLowRes = GetUniformLocation("heightMapLowRes");
            loc_blendFactor = GetUniformLocation("blendFactor");
            loc_heightScale = GetUniformLocation("heightScale");

            loc_riverMask = GetUniformLocation("riverMask");

            loc_riverHeightOffset = GetUniformLocation("riverHeightOffset");
        }

        // --- Load 메서드 ---

        public void LoadModelMatrix(Matrix4x4f model) => LoadUniformMatrix4(loc_model, model);
        public void LoadBlendFactor(float value) => Gl.Uniform1(loc_blendFactor, value);
        public void LoadHeightScale(float value) => Gl.Uniform1(loc_heightScale, value);
        public void LoadRiverHeightOffset(float value) => Gl.Uniform1(loc_riverHeightOffset, value);

        // Unit 0: 고해상도 높이맵
        public void LoadHeightHighResMap(uint texture)
        {
            Gl.Uniform1(loc_heightMapHighRes, 0);
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        // Unit 1: 저해상도 높이맵
        public void LoadHeightLowResMap(uint texture)
        {
            Gl.Uniform1(loc_heightMapLowRes, 1);
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        // Unit 2: 강 마스크 (TCS 패치 컬링 + TES 버텍스 추방용)
        public void LoadRiverMask(uint texture)
        {
            Gl.Uniform1(loc_riverMask, 2);
            Gl.ActiveTexture(TextureUnit.Texture2);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }
    }
}
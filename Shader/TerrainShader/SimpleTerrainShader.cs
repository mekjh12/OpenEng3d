using OpenGL;
using Common;

namespace Shader
{
    /// <summary>
    /// 지형의 간단한 렌더링을 위한 셰이더입니다. (Zero-allocation)
    /// 테셀레이션을 사용하여 지형의 높이맵을 처리합니다.
    /// </summary>
    public class SimpleTerrainShader : ShaderProgramBase
    {
        /// <summary>버텍스 셰이더 파일 경로</summary>
        const string VERTEX_FILE = @"\Shader\TerrainShader\terrain.vert";
        /// <summary>프래그먼트 셰이더 파일 경로</summary>
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\simple.frag";
        /// <summary>테셀레이션 컨트롤 셰이더 파일 경로</summary>
        const string TCS_FILE = @"\Shader\TerrainShader\simple.tcs.glsl";
        /// <summary>테셀레이션 평가 셰이더 파일 경로</summary>
        const string TES_FILE = @"\Shader\TerrainShader\simple.tes.glsl";

        // ✅ 유니폼 위치 캐싱 (GC 할당 없음)
        private int loc_gReversedLightDir;
        private int loc_camPos;
        private int loc_heightScale;
        private int loc_model;
        private int loc_view;
        private int loc_proj;
        private int loc_gHeightMap;

        /// <summary>
        /// 간단한 지형 셰이더를 초기화합니다.
        /// </summary>
        /// <param name="projectPath">셰이더 파일이 위치한 프로젝트 경로</param>
        public SimpleTerrainShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            TcsFileName = projectPath + TCS_FILE;
            TesFileName = projectPath + TES_FILE;
            InitCompileShader();
        }

        /// <summary>
        /// 셰이더의 유니폼 변수 위치를 가져옵니다.
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            loc_gReversedLightDir = GetUniformLocation("gReversedLightDir");
            loc_camPos = GetUniformLocation("camPos");
            loc_heightScale = GetUniformLocation("heightScale");
            loc_model = GetUniformLocation("model");
            loc_view = GetUniformLocation("view");
            loc_proj = GetUniformLocation("proj");
            loc_gHeightMap = GetUniformLocation("gHeightMap");
        }

        /// <summary>
        /// 셰이더의 입력 애트리뷰트를 바인딩합니다.
        /// 테셀레이션 셰이더에서 처리하므로 버텍스 셰이더의 애트리뷰트는 비활성화됩니다.
        /// </summary>
        protected override void BindAttributes()
        {
            //base.BindAttribute(0, "position");
        }

        #region Uniform 로딩 함수 (Zero-allocation)

        public void LoadTexture(TextureUnit textureUnit, uint texture)
        {
            int ind = textureUnit - TextureUnit.Texture0;
            //base.LoadInt(_location[textureUniformName.ToString()], ind);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        /// <summary>역방향 광원 방향을 설정합니다.</summary>
        public void LoadReversedLightDirection(in Vertex3f direction)
        {
            Gl.Uniform3(loc_gReversedLightDir, direction.x, direction.y, direction.z);
        }

        /// <summary>카메라 위치를 설정합니다.</summary>
        public void LoadCameraPosition(in Vertex3f position)
        {
            Gl.Uniform3(loc_camPos, position.x, position.y, position.z);
        }

        /// <summary>높이 스케일 값을 설정합니다.</summary>
        public void LoadHeightScale(float scale)
        {
            Gl.Uniform1(loc_heightScale, scale);
        }

        /// <summary>모델 변환 행렬을 설정합니다.</summary>
        public void LoadModelMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_model, matrix);
        }

        /// <summary>뷰 변환 행렬을 설정합니다.</summary>
        public void LoadViewMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_view, matrix);
        }

        /// <summary>투영 변환 행렬을 설정합니다.</summary>
        public void LoadProjectionMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_proj, matrix);
        }

        /// <summary>
        /// 높이맵 텍스처를 바인딩합니다.
        /// </summary>
        /// <param name="textureUnit">텍스처 유닛</param>
        /// <param name="texture">텍스처 ID</param>
        public void LoadHeightMap(TextureUnit textureUnit, uint texture)
        {
            int textureIndex = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_gHeightMap, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        #endregion
    }
}
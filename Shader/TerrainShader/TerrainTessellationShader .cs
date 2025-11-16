using OpenGL;
using Common;

namespace Shader
{
    /// <summary>
    /// 지형 렌더링을 위한 테셀레이션 셰이더입니다. (Zero-allocation)
    /// 높이맵 기반의 지형을 동적 LOD로 처리합니다.
    /// </summary>
    public class TerrainTessellationShader : ShaderProgramBase
    {
        /// <summary>버텍스 셰이더 파일 경로</summary>
        const string VERTEX_FILE = @"\Shader\TerrainShader\terrain.vert";
        /// <summary>프래그먼트 셰이더 파일 경로</summary>
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\terrain.frag";
        /// <summary>테셀레이션 컨트롤 셰이더 파일 경로</summary>
        const string TCS_FILE = @"\Shader\TerrainShader\terrain.tcs.glsl";
        /// <summary>테셀레이션 평가 셰이더 파일 경로</summary>
        const string TES_FILE = @"\Shader\TerrainShader\terrain.tes.glsl";

        // ✅ 유니폼 위치 캐싱 (GC 할당 없음)
        private int loc_heightScale;
        private int loc_model;
        private int loc_proj;
        private int loc_view;
        private int loc_camPos;
        private int loc_color;
        private int loc_isTextured;
        private int loc_gIsDetailMap;
        private int loc_gVegetationMap;
        private int loc_gLightDir;
        private int loc_isFogEnable;
        private int loc_fogColor;
        private int loc_fogDensity;
        private int loc_fogPlane;

        /// <summary>
        /// 지형 테셀레이션 셰이더를 초기화합니다.
        /// </summary>
        /// <param name="projectPath">셰이더 파일이 위치한 프로젝트 경로</param>
        public TerrainTessellationShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFilename = projectPath + FRAGMENT_FILE;
            TcsFilename = projectPath + TCS_FILE;
            TesFilename = projectPath + TES_FILE;
            InitCompileShader();
        }

        /// <summary>
        /// 셰이더의 입력 애트리뷰트를 바인딩합니다.
        /// </summary>
        protected override void BindAttributes()
        {
            base.BindAttribute(0, "position");    // 위치
            base.BindAttribute(1, "texCoord");    // 텍스처 좌표
            base.BindAttribute(2, "color");       // 색상
        }

        /// <summary>
        /// 셰이더의 모든 유니폼 변수 위치를 가져옵니다.
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            loc_heightScale = GetUniformLocation("heightScale");
            loc_model = GetUniformLocation("model");
            loc_proj = GetUniformLocation("proj");
            loc_view = GetUniformLocation("view");
            loc_camPos = GetUniformLocation("camPos");
            loc_color = GetUniformLocation("color");
            loc_isTextured = GetUniformLocation("isTextured");
            loc_gIsDetailMap = GetUniformLocation("gIsDetailMap");
            loc_gVegetationMap = GetUniformLocation("gVegetationMap");
            loc_gLightDir = GetUniformLocation("gLightDir");
            loc_isFogEnable = GetUniformLocation("isFogEnable");
            loc_fogColor = GetUniformLocation("fogColor");
            loc_fogDensity = GetUniformLocation("fogDensity");
            loc_fogPlane = GetUniformLocation("fogPlane");
        }

        #region Uniform 로딩 함수 (Zero-allocation)

        /// <summary>높이 스케일 값을 설정합니다.</summary>
        public void LoadHeightScale(float value)
        {
            Gl.Uniform1(loc_heightScale, value);
        }

        /// <summary>모델 변환 행렬을 설정합니다.</summary>
        public void LoadModelMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_model, matrix);
        }

        /// <summary>투영 변환 행렬을 설정합니다.</summary>
        public void LoadProjectionMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_proj, matrix);
        }

        /// <summary>뷰 변환 행렬을 설정합니다.</summary>
        public void LoadViewMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_view, matrix);
        }

        /// <summary>카메라 위치를 설정합니다.</summary>
        public void LoadCameraPosition(in Vertex3f position)
        {
            Gl.Uniform3(loc_camPos, position.x, position.y, position.z);
        }

        /// <summary>지형 베이스 색상을 설정합니다.</summary>
        public void LoadColor(in Vertex4f color)
        {
            Gl.Uniform4(loc_color, color.x, color.y, color.z, color.w);
        }

        /// <summary>텍스처 사용 여부를 설정합니다.</summary>
        public void LoadIsTextured(bool value)
        {
            Gl.Uniform1(loc_isTextured, value ? 1 : 0);
        }

        /// <summary>디테일맵 사용 여부를 설정합니다.</summary>
        public void LoadIsDetailMap(bool value)
        {
            Gl.Uniform1(loc_gIsDetailMap, value ? 1 : 0);
        }

        /// <summary>광원 방향을 설정합니다.</summary>
        public void LoadLightDirection(in Vertex3f direction)
        {
            Gl.Uniform3(loc_gLightDir, direction.x, direction.y, direction.z);
        }

        /// <summary>안개 효과 사용 여부를 설정합니다.</summary>
        public void LoadIsFogEnable(bool value)
        {
            Gl.Uniform1(loc_isFogEnable, value ? 1 : 0);
        }

        /// <summary>안개 색상을 설정합니다.</summary>
        public void LoadFogColor(in Vertex3f color)
        {
            Gl.Uniform3(loc_fogColor, color.x, color.y, color.z);
        }

        /// <summary>안개 밀도를 설정합니다.</summary>
        public void LoadFogDensity(float value)
        {
            Gl.Uniform1(loc_fogDensity, value);
        }

        /// <summary>안개 평면을 설정합니다.</summary>
        public void LoadFogPlane(in Vertex2f plane)
        {
            Gl.Uniform2(loc_fogPlane, plane.x, plane.y);
        }

        /// <summary>
        /// 텍스처를 바인딩합니다.
        /// </summary>
        /// <param name="textureUnit">텍스처 유닛</param>
        /// <param name="texture">텍스처 ID</param>
        public void LoadTexture(TextureUnit textureUnit, uint texture)
        {
            int textureIndex = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_isTextured, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        /// <summary>
        /// 식생 맵 텍스처를 바인딩합니다.
        /// </summary>
        /// <param name="textureUnit">텍스처 유닛</param>
        /// <param name="texture">텍스처 ID</param>
        public void LoadVegetationMap(TextureUnit textureUnit, uint texture)
        {
            int textureIndex = textureUnit - TextureUnit.Texture0;
            Gl.Uniform1(loc_gVegetationMap, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }

        #endregion
    }
}
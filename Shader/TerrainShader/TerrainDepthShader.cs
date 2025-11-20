using OpenGL;
using Common;
using System;

namespace Shader
{
    /// <summary>
    /// 지형의 깊이맵 생성을 위한 셰이더입니다. (Zero-allocation)
    /// 테셀레이션을 사용하여 지형의 높이맵을 처리하고 깊이값만 계산합니다.
    /// </summary>
    public class TerrainDepthShader : ShaderProgramBase
    {
        /// <summary>버텍스 셰이더 파일 경로</summary>
        const string VERTEX_FILE = @"\Shader\TerrainShader\terrain.vert";
        /// <summary>테셀레이션 컨트롤 셰이더 파일 경로</summary>
        const string TCS_FILE = @"\Shader\TerrainShader\terrain.tcs.glsl";
        /// <summary>테셀레이션 평가 셰이더 파일 경로</summary>
        const string TES_FILE = @"\Shader\TerrainShader\terrain.tes.glsl";
        /// <summary>프래그먼트 셰이더 파일 경로 (빈 셰이더)</summary>
        const string FRAGMENT_FILE = @"\Shader\TerrainShader\null.frag";

        // ✅ 유니폼 위치 캐싱
        private int loc_heightScale;
        private int loc_model;
        private int loc_view;
        private int loc_proj;
        private int loc_gHeightMap;

        /// <summary>
        /// 지형 깊이맵 셰이더를 초기화합니다.
        /// </summary>
        /// <param name="projectPath">셰이더 파일이 위치한 프로젝트 경로</param>
        public TerrainDepthShader(string projectPath) : base()
        {
            // 셰이더 초기화
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;
            TcsFileName = projectPath + TCS_FILE;
            TesFileName = projectPath + TES_FILE;

            // 컴파일 및 링크
            InitCompileShader();
        }

        /// <summary>
        /// 셰이더의 유니폼 변수 위치를 가져옵니다.
        /// 변환 행렬과 높이맵 텍스처의 위치를 조회합니다.
        /// </summary>
        protected override void GetAllUniformLocations()
        {
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

        /// <summary>
        /// 높이 스케일 값을 설정합니다.
        /// </summary>
        public void LoadHeightScale(float scale)
        {
            LoadUniform1f(loc_heightScale, scale);
        }

        /// <summary>
        /// 모델 변환 행렬을 설정합니다.
        /// </summary>
        public void LoadModelMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_model, matrix);
        }

        /// <summary>
        /// 뷰 변환 행렬을 설정합니다.
        /// </summary>
        public void LoadViewMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_view, matrix);
        }

        /// <summary>
        /// 투영 변환 행렬을 설정합니다.
        /// </summary>
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
            LoadUniform1i(loc_gHeightMap, textureIndex);
            Gl.ActiveTexture(textureUnit);
            Gl.BindTexture(TextureTarget.Texture2d, texture);
        }
    }
}
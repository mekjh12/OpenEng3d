using System;
using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 구름 생성을 위한 컴퓨트 셰이더
    /// </summary>
    public class CloudShader : ShaderProgram<CloudShader.UNIFORM_NAME>
    {
        /// <summary>
        /// 유니폼 변수 열거형
        /// </summary>
        public enum UNIFORM_NAME
        {
            // 태양 관련 유니폼
            sunPosition,          // 태양 위치 (정규화된 방향 벡터)
            sunColor,             // 태양 색상

            // 구름 형상 관련 유니폼
            cloudCoverage,        // 구름 커버리지 (0.0 - 1.0)
            cloudBaseAltitude,    // 구름 바닥 고도 (0.0 - 1.0, 0 = 지평선, 1 = 천정)
            cloudTopAltitude,     // 구름 최상층 고도 (0.0 - 1.0)
            cloudFeatheringDistance, // 구름 경계면 페더링 거리 (0.0 - 0.2)

            // 구름 품질 관련 유니폼
            cloudDensity,         // 구름 밀도 (0.0 - 2.0)
            cloudDetail,          // 구름 디테일 수준 (0.0 - 2.0)

            // 구름 애니메이션 관련 유니폼
            cloudOffset,          // 구름 오프셋 (구름 위치 조정)
            time,                 // 시간 변수 (애니메이션용)
            randomSeed,           // 랜덤 시드

            // 총 유니폼 개수
            Count
        }

        // 컴퓨트 셰이더 파일 경로
        const string COMPUTE_FILE = @"\Shader\SkyDomeShader\cloudGenerator.comp";

        // 텍스처 속성
        private uint _cloudTextureId;
        private readonly int _texWidth;
        private readonly int _texHeight;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="projectPath">프로젝트 경로</param>
        public CloudShader(string projectPath, int width, int height) : base()
        {
            _name = this.GetType().Name;
            _texWidth = width;
            _texHeight = height;
            
            // 컴퓨트 셰이더 파일 경로 설정
            _compFilename = projectPath + COMPUTE_FILE;

            // 셰이더 초기화
            InitCompileShader();

            // 텍스처 초기화
            InitializeTexture();

            // 기본 유니폼 값 설정
            SetDefaultUniforms();
        }


        /// <summary>
        /// 텍스처 초기화 메서드
        /// </summary>
        private void InitializeTexture()
        {
            // 텍스처 생성
            _cloudTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _cloudTextureId);

            // 텍스처 설정
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

            // 텍스처 데이터 할당 (초기 값은 빈 상태)
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f,
                         _texWidth, _texHeight, 0,
                         OpenGL.PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

            Gl.BindTexture(TextureTarget.Texture2d, 0);
        }

        protected override void BindAttributes()
        {
            // 컴퓨트 셰이더는 BindAttributes가 필요 없음
        }

        protected override void GetAllUniformLocations()
        {
            // 유니폼 변수 이름을 이용하여 위치 찾기
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }

        /// <summary>
        /// 기본 유니폼 값 설정
        /// </summary>
        private void SetDefaultUniforms()
        {
            Bind();

            // 태양 관련 기본 유니폼 설정
            LoadUniform(UNIFORM_NAME.sunColor, new Vertex3f(1.0f, 0.95f, 0.8f));

            // 구름 형상 관련 기본 유니폼 설정
            LoadUniform(UNIFORM_NAME.cloudCoverage, 0.5f);
            LoadUniform(UNIFORM_NAME.cloudBaseAltitude, 0.1f);
            LoadUniform(UNIFORM_NAME.cloudTopAltitude, 0.3f);
            LoadUniform(UNIFORM_NAME.cloudFeatheringDistance, 0.03f);

            // 구름 품질 관련 기본 유니폼 설정
            LoadUniform(UNIFORM_NAME.cloudDensity, 0.5f);
            LoadUniform(UNIFORM_NAME.cloudDetail, 1.0f);

            // 구름 애니메이션 관련 기본 유니폼 설정
            LoadUniform(UNIFORM_NAME.cloudOffset, new Vertex3f(0.0f, 0.0f, 0.0f));
            LoadUniform(UNIFORM_NAME.time, 0.0f);
            LoadUniform(UNIFORM_NAME.randomSeed, new Vertex4f(0.123f, 0.456f, 0.789f, 0.0f));

            Unbind();
        }

        /// <summary>
        /// 구름 텍스처 렌더링
        /// </summary>
        /// <param name="skyTextureId">입력 하늘색 텍스처 ID</param>
        /// <param name="finalTextureId">출력 최종 텍스처 ID</param>
        /// <param name="sunPosition">태양 위치</param>
        /// <param name="cloudCoverage">구름 커버리지 (0.0-1.0)</param>
        /// <param name="cloudBaseAltitude">구름 바닥 고도 (0.0-1.0)</param>
        /// <param name="cloudTopAltitude">구름 상단 고도 (0.0-1.0)</param>
        /// <param name="cloudOffset">구름 오프셋</param>
        public void RenderCloudTexture(
            uint skyTextureId,
            uint finalTextureId,
            Vertex3f sunPosition,
            float cloudCoverage = 0.5f,
            float cloudBaseAltitude = 0.1f,
            float cloudTopAltitude = 0.3f,
            Vertex3f? cloudOffset = null)
        {
            // 기본 오프셋 제공
            if (cloudOffset == null)
                cloudOffset = new Vertex3f(0.0f, 0.0f, 0.0f);

            Bind();

            // 태양 위치 유니폼 설정
            LoadUniform(UNIFORM_NAME.sunPosition, sunPosition);

            // 구름 형상 관련 유니폼 설정
            LoadUniform(UNIFORM_NAME.cloudCoverage, cloudCoverage);
            LoadUniform(UNIFORM_NAME.cloudBaseAltitude, cloudBaseAltitude);
            LoadUniform(UNIFORM_NAME.cloudTopAltitude, cloudTopAltitude);

            // 구름 오프셋 설정
            LoadUniform(UNIFORM_NAME.cloudOffset, (Vertex3f)cloudOffset);

            // 이미지 바인딩
            Gl.BindImageTexture(0, skyTextureId, 0, false, 0, BufferAccess.ReadOnly, InternalFormat.Rgba16f);
            Gl.BindImageTexture(1, finalTextureId, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);

            // 계산 셰이더 디스패치
            Gl.DispatchCompute((uint)(_texWidth / 16) + 1, (uint)(_texHeight / 16) + 1, 1);

            // 메모리 배리어 (계산 완료 대기)
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            Unbind();
        }
    }
}
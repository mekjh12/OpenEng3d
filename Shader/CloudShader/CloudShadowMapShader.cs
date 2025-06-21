using Common;
using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// 구름 그림자 맵을 미리 계산하는 컴퓨트 셰이더
    /// </summary>
    public class CloudShadowMapShader : ShaderProgram<CloudShadowMapShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            cloudTexture,    // 구름 3D 텍스처
            lightDir,        // 광원 방향
            resolution,      // 텍스처 해상도
            cloudDensity,    // 구름 밀도
            Count
        }

        const string COMPUTE_FILE = @"\Shader\CloudShader\shadowMap.comp";
        const int WORK_GROUP_SIZE = 8;

        // 3D 텍스처 관련 변수
        private uint _shadowMapHandle;
        private int _textureSize;
        private Vertex3f _currentLightDir;
        private float _currentDensity;
        private bool _isDirty = true;  // true면 업데이트 필요

        // 프로퍼티
        public uint ShadowMapHandle => _shadowMapHandle;
        public int TextureSize => _textureSize;

        /// <summary>
        /// 그림자 맵 컴퓨트 셰이더 생성자
        /// </summary>
        public CloudShadowMapShader(string computeShaderPath, int textureSize = 128) : base()
        {
            _name = GetType().Name;
            _textureSize = textureSize;
            _currentLightDir = new Vertex3f(0, 0, 0);
            _currentDensity = 1.0f;

            // 셰이더 파일 경로 설정
            ComputeFileName = computeShaderPath + COMPUTE_FILE;

            // 그림자 맵 텍스처 초기화
            InitializeShadowMap();

            // 셰이더 컴파일 및 초기화
            InitCompileShader();
        }

        /// <summary>
        /// 그림자 맵 텍스처 초기화
        /// </summary>
        private void InitializeShadowMap()
        {
            // 텍스처 생성
            _shadowMapHandle = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture3d, _shadowMapHandle);

            // 텍스처 파라미터 설정
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture3d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapR, Gl.CLAMP_TO_EDGE);

            // R32F 포맷 사용 (투과율 값 하나만 저장)
            Gl.TexImage3D(TextureTarget.Texture3d, 0, InternalFormat.R32f,
                _textureSize, _textureSize, _textureSize, 0,
                PixelFormat.Red, PixelType.Float, IntPtr.Zero);

            // 초기값 1.0f로 설정 (완전 투명)
            float[] clearValue = new float[] { 1.0f };
            Gl.ClearTexImage(_shadowMapHandle, 0, PixelFormat.Red, PixelType.Float, clearValue);

            // 바인딩 해제
            Gl.BindTexture(TextureTarget.Texture3d, 0);
        }

        /// <summary>
        /// 컴퓨트 셰이더 실행 - 그림자 맵 계산
        /// </summary>
        public void ComputeShadowMap(uint cloudTextureId, Vertex3f lightDir, float cloudDensity)
        {
            // 광원 방향이나 밀도가 변경되었는지 확인
            bool lightDirChanged = !_currentLightDir.Equals(lightDir);
            bool densityChanged = Math.Abs(_currentDensity - cloudDensity) > 0.001f;

            // 변경이 없으면 재계산 안함
            if (!_isDirty && !lightDirChanged && !densityChanged)
                return;

            // 현재 값 업데이트
            _currentLightDir = lightDir;
            _currentDensity = cloudDensity;
            _isDirty = false;

            // 셰이더 프로그램 사용 시작
            Bind();

            // 입력 텍스처(구름 텍스처) 바인딩
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture3d, cloudTextureId);
            SetInt("cloudTexture", 0);

            // 출력 이미지(그림자 맵) 바인딩
            Gl.BindImageTexture(0, _shadowMapHandle, 0, true, 0, BufferAccess.WriteOnly, InternalFormat.R32f);

            // 유니폼 변수 설정
            LoadUniform(UNIFORM_NAME.lightDir, lightDir.Normalized);
            LoadUniform(UNIFORM_NAME.resolution, new Vertex3f(_textureSize, _textureSize, _textureSize));
            LoadUniform(UNIFORM_NAME.cloudDensity, cloudDensity);

            // 워크 그룹 계산 및 디스패치
            int groupsX = (int)Math.Ceiling(_textureSize / (float)WORK_GROUP_SIZE);
            int groupsY = (int)Math.Ceiling(_textureSize / (float)WORK_GROUP_SIZE);
            int groupsZ = (int)Math.Ceiling(_textureSize / (float)WORK_GROUP_SIZE);

            Console.WriteLine($"그림자 맵 계산 중: ({groupsX}, {groupsY}, {groupsZ}) 그룹");

            Gl.DispatchCompute((uint)groupsX, (uint)groupsY, (uint)groupsZ);

            // 메모리 배리어로 쓰기 작업 완료 대기
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            // 셰이더 프로그램 사용 종료
            Unbind();

            Console.WriteLine("그림자 맵 계산 완료");
        }

        /// <summary>
        /// 강제로 다음 프레임에 그림자맵 업데이트하도록 설정
        /// </summary>
        public void SetDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// 컴퓨트 셰이더는 BindAttributes가 필요 없음
        /// </summary>
        protected override void BindAttributes()
        {
            // 컴퓨트 셰이더는 구현 필요 없음
        }

        /// <summary>
        /// 모든 유니폼 위치 가져오기
        /// </summary>
        protected override void GetAllUniformLocations()
        {
            for (int i = 0; i < (int)UNIFORM_NAME.Count; i++)
            {
                UniformLocation(((UNIFORM_NAME)i).ToString());
            }
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public override void CleanUp()
        {
            // 그림자 맵 텍스처 삭제
            if (_shadowMapHandle != 0)
            {
                Gl.DeleteTextures(_shadowMapHandle);
                _shadowMapHandle = 0;
            }

            // 부모 클래스의 CleanUp 호출
            base.CleanUp();
        }
    }
}
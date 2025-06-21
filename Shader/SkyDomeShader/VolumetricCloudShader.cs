using System;
using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 볼륨 레이트레이싱 기반 구름 생성을 위한 컴퓨트 셰이더
    /// </summary>
    public class VolumetricCloudShader : ShaderProgram<VolumetricCloudShader.UNIFORM_NAME>
    {
        /// <summary>
        /// 유니폼 변수 열거형
        /// </summary>
        public enum UNIFORM_NAME
        {
            // 텍스처 유니폼 (샘플러)
            noiseTexture,           // 3D 노이즈 텍스처
            detailNoiseTexture,     // 3D 디테일 노이즈 텍스처

            // 태양 관련 유니폼
            sunDirection,           // 태양 위치 (정규화된 방향 벡터)
            sunColor,               // 태양 색상

            // 카메라/관찰자 관련 유니폼
            cameraPosition,         // 카메라 위치
            nearPlane,              // 카메라 가까운 평면
            farPlane,               // 카메라 먼 평면

            // 구름 형상 관련 유니폼
            cloudLayerBottom,       // 구름층 하단 고도
            cloudLayerTop,          // 구름층 상단 고도
            cloudCoverage,          // 구름 커버리지 (0.0-1.0)
            cloudDensity,           // 구름 밀도 계수
            cloudSharpness,         // 구름 선명도
            cloudDetailStrength,    // 구름 디테일 강도

            // 구름 애니메이션 관련 유니폼
            cloudOffset,            // 구름 오프셋 (애니메이션용)
            time,                   // 시간

            // 레이트레이싱 관련 유니폼
            primaryStepCount,       // 주 레이마칭 단계 수
            lightStepCount,         // 광선 샘플링 단계 수
            primaryStepSize,        // 주 레이마칭 단계 크기
            lightStepSize,          // 광선 샘플링 단계 크기

            // 총 유니폼 개수
            Count
        }

        // 컴퓨트 셰이더 파일 경로
        const string COMPUTE_FILE = @"\Shader\SkyDomeShader\volumetric_cloud.comp";

        // 3D 노이즈 텍스처 ID
        private uint _noiseTexture;
        private uint _detailNoiseTexture;

        // 텍스처 크기
        private int _noiseSize;

        // 구름층 매개변수 (미터 단위)
        private float _cloudLayerBottom = 100.0f;     // 구름층 하단 고도
        private float _cloudLayerTop = 2000.0f;        // 구름층 상단 고도

        // 레이트레이싱 매개변수
        private int _primaryStepCount = 64;            // 주 레이마칭 단계 수
        private int _lightStepCount = 6;               // 광선 샘플링 단계 수
        private float _primaryStepSize = 100.0f;       // 주 레이마칭 단계 크기 (미터)
        private float _lightStepSize = 200.0f;         // 광선 샘플링 단계 크기 (미터)

        private readonly int _texWidth;
        private readonly int _texHeight;

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="projectPath">프로젝트 경로</param>
        /// <param name="width">출력 텍스처 너비</param>
        /// <param name="height">출력 텍스처 높이</param>
        /// <param name="noiseSize">3D 노이즈 텍스처 크기</param>
        public VolumetricCloudShader(string projectPath, int width, int height, int noiseSize = 128) : base()
        {
            _name = this.GetType().Name;
            _noiseSize = noiseSize;
            _texWidth = width;
            _texHeight = height;

            // 컴퓨트 셰이더 파일 경로 설정
            _compFilename = projectPath + COMPUTE_FILE;

            // 셰이더 초기화
            InitCompileShader();

            // 3D 노이즈 텍스처 생성
            _noiseTexture = GenerateNoiseTexture3D(noiseSize, 1.0f, 0.5f);
            _detailNoiseTexture = GenerateNoiseTexture3D(noiseSize, 4.0f, 0.7f);

            // 기본 유니폼 값 설정
            SetDefaultUniforms();
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
        /// 3D 노이즈 텍스처 생성
        /// </summary>
        /// <param name="size">텍스처 크기 (size x size x size)</param>
        /// <param name="frequency">노이즈 주파수</param>
        /// <param name="persistence">노이즈 지속성</param>
        private uint GenerateNoiseTexture3D(int size, float frequency, float persistence)
        {
            uint textureID = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture3d, textureID);

            // 단일 채널 R16F 포맷으로 텍스처 생성
            Gl.TexImage3D(TextureTarget.Texture3d, 0, InternalFormat.R16f, size, size, size, 0,
                        OpenGL.PixelFormat.Red, PixelType.Float, IntPtr.Zero);

            // 텍스처 파라미터 설정
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureMinFilter, TextureMinFilter.Linear);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureMagFilter, TextureMagFilter.Linear);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapS, TextureWrapMode.Repeat);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapT, TextureWrapMode.Repeat);
            Gl.TexParameteri(TextureTarget.Texture3d, TextureParameterName.TextureWrapR, TextureWrapMode.Repeat);

            // 노이즈 데이터 생성
            float[] noiseData = new float[size * size * size];
            GeneratePerlinNoise3D(noiseData, size, frequency, persistence);

            // 노이즈 데이터 업로드
            System.IntPtr ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(noiseData.Length * sizeof(float));
            try
            {
                System.Runtime.InteropServices.Marshal.Copy(noiseData, 0, ptr, noiseData.Length);
                Gl.TexSubImage3D(TextureTarget.Texture3d, 0, 0, 0, 0, size, size, size,
                               OpenGL.PixelFormat.Red, PixelType.Float, ptr);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
            }

            // 텍스처 바인딩 해제
            Gl.BindTexture(TextureTarget.Texture3d, 0);

            return textureID;
        }

        /// <summary>
        /// 3D 펄린 노이즈 생성
        /// </summary>
        private void GeneratePerlinNoise3D(float[] data, int size, float frequency, float persistence)
        {
            // 노이즈 생성을 위한 임시 랜덤 객체
            Random rand = new Random(42); // 일관된 결과를 위해 고정된 시드 사용

            // 그래디언트 벡터 테이블 생성 (256개의 랜덤 방향 벡터)
            int gradSize = 256;
            Vertex3f[] gradients = new Vertex3f[gradSize];

            for (int i = 0; i < gradSize; i++)
            {
                float theta = (float)(rand.NextDouble() * 2.0 * Math.PI);
                float phi = (float)(rand.NextDouble() * Math.PI);

                float x = (float)(Math.Sin(phi) * Math.Cos(theta));
                float y = (float)(Math.Sin(phi) * Math.Sin(theta));
                float z = (float)(Math.Cos(phi));

                gradients[i] = new Vertex3f(x, y, z);
            }

            // 퍼뮤테이션 테이블 생성
            int[] p = new int[512];
            for (int i = 0; i < 256; i++)
                p[i] = i;

            // 퍼뮤테이션 테이블 섞기 (Fisher-Yates 셔플)
            for (int i = 255; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                int temp = p[i];
                p[i] = p[j];
                p[j] = temp;
            }

            // 퍼뮤테이션 테이블 복제 (인덱스 연산 단순화를 위해)
            for (int i = 0; i < 256; i++)
                p[i + 256] = p[i];

            // 노이즈 계산
            int octaves = 4;  // 옥타브 수

            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        // 정규화된 좌표 (0.0-1.0)
                        float nx = (float)x / size;
                        float ny = (float)y / size;
                        float nz = (float)z / size;

                        // FBM(Fractal Brownian Motion) 노이즈 계산
                        float noise = 0.0f;
                        float amplitude = 1.0f;
                        float totalAmplitude = 0.0f;
                        float currentFrequency = frequency;

                        for (int o = 0; o < octaves; o++)
                        {
                            // 현재 옥타브의 노이즈 계산
                            float octaveNoise = PerlinNoise3D(nx * currentFrequency, ny * currentFrequency, nz * currentFrequency, p, gradients);

                            // 노이즈 값 누적
                            noise += octaveNoise * amplitude;
                            totalAmplitude += amplitude;

                            // 다음 옥타브 준비
                            amplitude *= persistence;
                            currentFrequency *= 2.0f;
                        }

                        // 노이즈 값 정규화 및 저장
                        noise /= totalAmplitude;  // -1.0 ~ 1.0 범위
                        noise = (noise + 1.0f) * 0.5f; // 0.0 ~ 1.0 범위로 변환

                        // 결과 배열에 저장
                        int index = x + y * size + z * size * size;
                        data[index] = noise;
                    }
                }
            }
        }

        /// <summary>
        /// 3D 펄린 노이즈 계산
        /// </summary>
        private float PerlinNoise3D(float x, float y, float z, int[] p, Vertex3f[] gradients)
        {
            // 정수 부분과 소수 부분 분리
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            int Z = (int)Math.Floor(z) & 255;

            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);
            z -= (float)Math.Floor(z);

            // 페이드 곡선 (5차 에르미트 곡선)
            float u = Fade(x);
            float v = Fade(y);
            float w = Fade(z);

            // 해시 계산
            int A = p[X] + Y;
            int AA = p[A] + Z;
            int AB = p[A + 1] + Z;
            int B = p[X + 1] + Y;
            int BA = p[B] + Z;
            int BB = p[B + 1] + Z;

            // 그래디언트 해시 인덱스
            int gi000 = p[AA] & 255;
            int gi001 = p[AB] & 255;
            int gi010 = p[AA + 1] & 255;
            int gi011 = p[AB + 1] & 255;
            int gi100 = p[BA] & 255;
            int gi101 = p[BB] & 255;
            int gi110 = p[BA + 1] & 255;
            int gi111 = p[BB + 1] & 255;

            // 그래디언트
            Vertex3f g000 = gradients[gi000];
            Vertex3f g001 = gradients[gi001];
            Vertex3f g010 = gradients[gi010];
            Vertex3f g011 = gradients[gi011];
            Vertex3f g100 = gradients[gi100];
            Vertex3f g101 = gradients[gi101];
            Vertex3f g110 = gradients[gi110];
            Vertex3f g111 = gradients[gi111];

            // 내적 계산
            float n000 = Dot(g000, x, y, z);
            float n001 = Dot(g001, x, y, z - 1);
            float n010 = Dot(g010, x, y - 1, z);
            float n011 = Dot(g011, x, y - 1, z - 1);
            float n100 = Dot(g100, x - 1, y, z);
            float n101 = Dot(g101, x - 1, y, z - 1);
            float n110 = Dot(g110, x - 1, y - 1, z);
            float n111 = Dot(g111, x - 1, y - 1, z - 1);

            // 선형 보간
            float nx00 = Lerp(n000, n100, u);
            float nx01 = Lerp(n001, n101, u);
            float nx10 = Lerp(n010, n110, u);
            float nx11 = Lerp(n011, n111, u);

            float nxy0 = Lerp(nx00, nx10, v);
            float nxy1 = Lerp(nx01, nx11, v);

            return Lerp(nxy0, nxy1, w);
        }

        /// <summary>
        /// 페이드 함수 (5차 에르미트 곡선)
        /// </summary>
        private float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        /// <summary>
        /// 그래디언트 벡터와 방향 벡터의 내적
        /// </summary>
        private float Dot(Vertex3f g, float x, float y, float z)
        {
            return g.x * x + g.y * y + g.z * z;
        }

        /// <summary>
        /// 선형 보간
        /// </summary>
        private float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        /// <summary>
        /// 기본 유니폼 값 설정
        /// </summary>
        private void SetDefaultUniforms()
        {
            Bind();

            // 태양 관련 유니폼
            LoadUniform(UNIFORM_NAME.sunColor, new Vertex3f(1.0f, 0.95f, 0.8f));

            // 카메라 관련 유니폼
            LoadUniform(UNIFORM_NAME.nearPlane, 0.1f);
            LoadUniform(UNIFORM_NAME.farPlane, 10000.0f);

            // 구름층 관련 유니폼
            LoadUniform(UNIFORM_NAME.cloudLayerBottom, _cloudLayerBottom);
            LoadUniform(UNIFORM_NAME.cloudLayerTop, _cloudLayerTop);
            LoadUniform(UNIFORM_NAME.cloudCoverage, 0.5f);
            LoadUniform(UNIFORM_NAME.cloudDensity, 0.5f);
            LoadUniform(UNIFORM_NAME.cloudSharpness, 3.0f);
            LoadUniform(UNIFORM_NAME.cloudDetailStrength, 0.7f);

            // 구름 애니메이션 관련 유니폼
            LoadUniform(UNIFORM_NAME.cloudOffset, new Vertex3f(0.0f, 0.0f, 0.0f));
            LoadUniform(UNIFORM_NAME.time, 0.0f);

            // 레이트레이싱 관련 유니폼
            LoadUniform(UNIFORM_NAME.primaryStepCount, _primaryStepCount);
            LoadUniform(UNIFORM_NAME.lightStepCount, _lightStepCount);
            LoadUniform(UNIFORM_NAME.primaryStepSize, _primaryStepSize);
            LoadUniform(UNIFORM_NAME.lightStepSize, _lightStepSize);

            Unbind();
        }

        /// <summary>
        /// 노이즈 텍스처를 유니폼으로 설정
        /// </summary>
        private void SetNoiseTextures()
        {
            // 노이즈 텍스처 바인딩 - 텍스처 유닛 2번 (binding = 2)
            Gl.ActiveTexture(TextureUnit.Texture0 + 2);
            Gl.BindTexture(TextureTarget.Texture3d, _noiseTexture);
            LoadInt(_location[UNIFORM_NAME.noiseTexture.ToString()], 2);

            // 디테일 노이즈 텍스처 바인딩 - 텍스처 유닛 3번 (binding = 3)
            Gl.ActiveTexture(TextureUnit.Texture0 + 3);
            Gl.BindTexture(TextureTarget.Texture3d, _detailNoiseTexture);
            LoadInt(_location[UNIFORM_NAME.detailNoiseTexture.ToString()], 3);

            // 텍스처 유닛 리셋
            Gl.ActiveTexture(TextureUnit.Texture0);
        }

        /// <summary>
        /// 구름 텍스처 렌더링
        /// </summary>
        /// <param name="skyTextureId">입력 하늘색 텍스처 ID</param>
        /// <param name="cloudTextureId">출력 구름 텍스처 ID</param>
        /// <param name="sunDirection">태양 방향</param>
        /// <param name="cameraPosition">카메라 위치</param>
        /// <param name="cloudCoverage">구름 커버리지 (0.0-1.0)</param>
        /// <param name="cloudDensity">구름 밀도 (0.0-1.0)</param>
        /// <param name="cloudSharpness">구름 선명도 (0.0-10.0)</param>
        /// <param name="cloudDetailStrength">구름 디테일 강도 (0.0-1.0)</param>
        /// <param name="cloudOffset">구름 오프셋</param>
        public void RenderCloudTexture(
            uint skyTextureId,
            uint cloudTextureId,
            Vertex3f sunDirection,
            Vertex3f cameraPosition,
            float cloudCoverage = 0.99f,
            float cloudDensity = 0.99f,
            float cloudSharpness = 3.0f,
            float cloudDetailStrength = 0.7f,
            Vertex3f? cloudOffset = null)
        {
            // 기본 오프셋 제공
            if (cloudOffset == null)
                cloudOffset = new Vertex3f(0.0f, 0.0f, 0.0f);

            Bind();

            // 태양 및 카메라 관련 유니폼 설정
            LoadUniform(UNIFORM_NAME.sunDirection, sunDirection);
            LoadUniform(UNIFORM_NAME.cameraPosition, cameraPosition);

            // 구름 형상 관련 유니폼 설정
            LoadUniform(UNIFORM_NAME.cloudCoverage, cloudCoverage);
            LoadUniform(UNIFORM_NAME.cloudDensity, cloudDensity);
            LoadUniform(UNIFORM_NAME.cloudSharpness, cloudSharpness);
            LoadUniform(UNIFORM_NAME.cloudDetailStrength, cloudDetailStrength);

            // 구름 오프셋 설정
            LoadUniform(UNIFORM_NAME.cloudOffset, (Vertex3f)cloudOffset);

            // 시간 설정 (0 = 움직이지 않음)
            LoadUniform(UNIFORM_NAME.time, 0.0f);

            // 노이즈 텍스처 설정 (3D 텍스처 바인딩)
            SetNoiseTextures();

            // 이미지 바인딩 (읽기/쓰기 텍스처)
            Gl.BindImageTexture(0, skyTextureId, 0, false, 0, BufferAccess.ReadOnly, InternalFormat.Rgba16f);
            Gl.BindImageTexture(1, cloudTextureId, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);

            // 계산 셰이더 디스패치 (16x16 워크그룹 크기 기반)
            uint groupsX = (uint)(_texWidth + 15) / 16;
            uint groupsY = (uint)(_texHeight + 15) / 16;
            Gl.DispatchCompute(groupsX, groupsY, 1);

            // 메모리 배리어 (계산 완료 대기)
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            // 텍스처 언바인딩
            Gl.ActiveTexture(TextureUnit.Texture0 + 3);
            Gl.BindTexture(TextureTarget.Texture3d, 0);

            Gl.ActiveTexture(TextureUnit.Texture0 + 2);
            Gl.BindTexture(TextureTarget.Texture3d, 0);

            Gl.ActiveTexture(TextureUnit.Texture0);

            Unbind();
        }

        /// <summary>
        /// 적운 구름 설정 (뭉게구름)
        /// </summary>
        public void SetCumulusCloud()
        {
            Bind();

            LoadUniform(UNIFORM_NAME.cloudCoverage, 0.45f);
            LoadUniform(UNIFORM_NAME.cloudDensity, 0.6f);
            LoadUniform(UNIFORM_NAME.cloudSharpness, 4.0f);
            LoadUniform(UNIFORM_NAME.cloudDetailStrength, 0.8f);

            Unbind();
        }

        /// <summary>
        /// 적란운 구름 설정 (뇌우 구름)
        /// </summary>
        public void SetCumulonimbusCloud()
        {
            Bind();

            LoadUniform(UNIFORM_NAME.cloudCoverage, 0.7f);
            LoadUniform(UNIFORM_NAME.cloudDensity, 0.9f);
            LoadUniform(UNIFORM_NAME.cloudSharpness, 2.5f);
            LoadUniform(UNIFORM_NAME.cloudDetailStrength, 0.9f);

            Unbind();
        }

        /// <summary>
        /// 층운 구름 설정 (낮은 회색 구름층)
        /// </summary>
        public void SetStratusCloud()
        {
            Bind();

            LoadUniform(UNIFORM_NAME.cloudCoverage, 0.8f);
            LoadUniform(UNIFORM_NAME.cloudDensity, 0.5f);
            LoadUniform(UNIFORM_NAME.cloudSharpness, 1.0f);
            LoadUniform(UNIFORM_NAME.cloudDetailStrength, 0.4f);

            Unbind();
        }

        /// <summary>
        /// 층적운 구름 설정 (낮은 뭉게구름 층)
        /// </summary>
        public void SetStratocumulusCloud()
        {
            Bind();

            LoadUniform(UNIFORM_NAME.cloudCoverage, 0.6f);
            LoadUniform(UNIFORM_NAME.cloudDensity, 0.45f);
            LoadUniform(UNIFORM_NAME.cloudSharpness, 1.1f);
            LoadUniform(UNIFORM_NAME.cloudDetailStrength, 1.1f);

            Unbind();
        }

        /// <summary>
        /// 권운 구름 설정 (높은 깃털 모양 구름)
        /// </summary>
        public void SetCirrusCloud()
        {
            Bind();

            LoadUniform(UNIFORM_NAME.cloudCoverage, 0.3f);
            LoadUniform(UNIFORM_NAME.cloudDensity, 0.2f);
            LoadUniform(UNIFORM_NAME.cloudSharpness, 6.0f);
            LoadUniform(UNIFORM_NAME.cloudDetailStrength, 0.9f);

            Unbind();
        }

        /// <summary>
        /// 맑은 하늘 설정 (구름 거의 없음)
        /// </summary>
        public void SetClearSky()
        {
            Bind();

            LoadUniform(UNIFORM_NAME.cloudCoverage, 0.05f);
            LoadUniform(UNIFORM_NAME.cloudDensity, 0.2f);
            LoadUniform(UNIFORM_NAME.cloudSharpness, 5.0f);
            LoadUniform(UNIFORM_NAME.cloudDetailStrength, 0.6f);

            Unbind();
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public override void CleanUp()
        {
            base.CleanUp();

            // 노이즈 텍스처 삭제
            if (_noiseTexture != 0)
            {
                Gl.DeleteTextures(new uint[] { _noiseTexture });
                _noiseTexture = 0;
            }

            if (_detailNoiseTexture != 0)
            {
                Gl.DeleteTextures(new uint[] { _detailNoiseTexture });
                _detailNoiseTexture = 0;
            }
        }
    }
}
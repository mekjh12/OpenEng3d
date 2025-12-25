using System;
using Common;
using OpenGL;

namespace Shader
{
    /// <summary>
    /// 볼륨 레이트레이싱 기반 구름 생성 컴퓨트 셰이더
    /// </summary>
    public class VolumetricCloudShader : ShaderProgramBase
    {
        // 컴퓨트 셰이더 파일 경로
        private const string COMPUTE_FILE = @"\Shader\SkyDomeShader\volumetric_cloud.comp";

        // 3D 노이즈 텍스처 ID
        private uint _noiseTexture;
        private uint _detailNoiseTexture;

        // 텍스처 크기
        private int _noiseSize;
        private readonly int _texWidth;
        private readonly int _texHeight;

        // 구름층 매개변수 (미터 단위)
        private float _cloudLayerBottom = 100.0f;     // 구름층 하단 고도
        private float _cloudLayerTop = 2000.0f;        // 구름층 상단 고도

        // 레이트레이싱 매개변수
        private int _primaryStepCount = 64;            // 주 레이마칭 단계 수
        private int _lightStepCount = 6;               // 광선 샘플링 단계 수
        private float _primaryStepSize = 100.0f;       // 주 레이마칭 단계 크기 (미터)
        private float _lightStepSize = 200.0f;         // 광선 샘플링 단계 크기 (미터)

        // 유니폼 위치 캐싱
        private int loc_noiseTexture;           // 3D 노이즈 텍스처
        private int loc_detailNoiseTexture;     // 3D 디테일 노이즈 텍스처
        private int loc_sunDirection;           // 태양 위치 (정규화된 방향 벡터)
        private int loc_sunColor;               // 태양 색상
        private int loc_cameraPosition;         // 카메라 위치
        private int loc_nearPlane;              // 카메라 가까운 평면
        private int loc_farPlane;               // 카메라 먼 평면
        private int loc_cloudLayerBottom;       // 구름층 하단 고도
        private int loc_cloudLayerTop;          // 구름층 상단 고도
        private int loc_cloudCoverage;          // 구름 커버리지 (0.0-1.0)
        private int loc_cloudDensity;           // 구름 밀도 계수
        private int loc_cloudSharpness;         // 구름 선명도
        private int loc_cloudDetailStrength;    // 구름 디테일 강도
        private int loc_cloudOffset;            // 구름 오프셋 (애니메이션용)
        private int loc_time;                   // 시간
        private int loc_primaryStepCount;       // 주 레이마칭 단계 수
        private int loc_lightStepCount;         // 광선 샘플링 단계 수
        private int loc_primaryStepSize;        // 주 레이마칭 단계 크기
        private int loc_lightStepSize;          // 광선 샘플링 단계 크기

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

            ComputeFileName = projectPath + COMPUTE_FILE;
            InitCompileShader();

            // 3D 노이즈 텍스처 생성
            _noiseTexture = GenerateNoiseTexture3D(noiseSize, 1.0f, 0.5f);
            _detailNoiseTexture = GenerateNoiseTexture3D(noiseSize, 4.0f, 0.7f);

            // 기본 유니폼 값 설정
            SetDefaultUniforms();
        }

        protected override void BindAttributes()
        {
            // 컴퓨트 셰이더는 attribute 바인딩 불필요
        }

        protected override void GetAllUniformLocations()
        {
            // 텍스처 유니폼
            loc_noiseTexture = GetUniformLocation("noiseTexture");
            loc_detailNoiseTexture = GetUniformLocation("detailNoiseTexture");

            // 태양 관련
            loc_sunDirection = GetUniformLocation("sunDirection");
            loc_sunColor = GetUniformLocation("sunColor");

            // 카메라/관찰자 관련
            loc_cameraPosition = GetUniformLocation("cameraPosition");
            loc_nearPlane = GetUniformLocation("nearPlane");
            loc_farPlane = GetUniformLocation("farPlane");

            // 구름 형상 관련
            loc_cloudLayerBottom = GetUniformLocation("cloudLayerBottom");
            loc_cloudLayerTop = GetUniformLocation("cloudLayerTop");
            loc_cloudCoverage = GetUniformLocation("cloudCoverage");
            loc_cloudDensity = GetUniformLocation("cloudDensity");
            loc_cloudSharpness = GetUniformLocation("cloudSharpness");
            loc_cloudDetailStrength = GetUniformLocation("cloudDetailStrength");

            // 구름 애니메이션 관련
            loc_cloudOffset = GetUniformLocation("cloudOffset");
            loc_time = GetUniformLocation("time");

            // 레이트레이싱 관련
            loc_primaryStepCount = GetUniformLocation("primaryStepCount");
            loc_lightStepCount = GetUniformLocation("lightStepCount");
            loc_primaryStepSize = GetUniformLocation("primaryStepSize");
            loc_lightStepSize = GetUniformLocation("lightStepSize");
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

            Gl.BindTexture(TextureTarget.Texture3d, 0);
            return textureID;
        }

        /// <summary>
        /// 3D 펄린 노이즈 생성
        /// </summary>
        private void GeneratePerlinNoise3D(float[] data, int size, float frequency, float persistence)
        {
            Random rand = new Random(42);
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

            int[] p = new int[512];
            for (int i = 0; i < 256; i++)
                p[i] = i;

            for (int i = 255; i > 0; i--)
            {
                int j = rand.Next(i + 1);
                int temp = p[i];
                p[i] = p[j];
                p[j] = temp;
            }

            for (int i = 0; i < 256; i++)
                p[i + 256] = p[i];

            int octaves = 4;

            for (int z = 0; z < size; z++)
            {
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float nx = (float)x / size;
                        float ny = (float)y / size;
                        float nz = (float)z / size;

                        float noise = 0.0f;
                        float amplitude = 1.0f;
                        float totalAmplitude = 0.0f;
                        float currentFrequency = frequency;

                        for (int o = 0; o < octaves; o++)
                        {
                            float octaveNoise = PerlinNoise3D(nx * currentFrequency, ny * currentFrequency, nz * currentFrequency, p, gradients);
                            noise += octaveNoise * amplitude;
                            totalAmplitude += amplitude;
                            amplitude *= persistence;
                            currentFrequency *= 2.0f;
                        }

                        noise /= totalAmplitude;
                        noise = (noise + 1.0f) * 0.5f;

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
            int X = (int)Math.Floor(x) & 255;
            int Y = (int)Math.Floor(y) & 255;
            int Z = (int)Math.Floor(z) & 255;

            x -= (float)Math.Floor(x);
            y -= (float)Math.Floor(y);
            z -= (float)Math.Floor(z);

            float u = Fade(x);
            float v = Fade(y);
            float w = Fade(z);

            int A = p[X] + Y;
            int AA = p[A] + Z;
            int AB = p[A + 1] + Z;
            int B = p[X + 1] + Y;
            int BA = p[B] + Z;
            int BB = p[B + 1] + Z;

            int gi000 = p[AA] & 255;
            int gi001 = p[AB] & 255;
            int gi010 = p[AA + 1] & 255;
            int gi011 = p[AB + 1] & 255;
            int gi100 = p[BA] & 255;
            int gi101 = p[BB] & 255;
            int gi110 = p[BA + 1] & 255;
            int gi111 = p[BB + 1] & 255;

            Vertex3f g000 = gradients[gi000];
            Vertex3f g001 = gradients[gi001];
            Vertex3f g010 = gradients[gi010];
            Vertex3f g011 = gradients[gi011];
            Vertex3f g100 = gradients[gi100];
            Vertex3f g101 = gradients[gi101];
            Vertex3f g110 = gradients[gi110];
            Vertex3f g111 = gradients[gi111];

            float n000 = Dot(g000, x, y, z);
            float n001 = Dot(g001, x, y, z - 1);
            float n010 = Dot(g010, x, y - 1, z);
            float n011 = Dot(g011, x, y - 1, z - 1);
            float n100 = Dot(g100, x - 1, y, z);
            float n101 = Dot(g101, x - 1, y, z - 1);
            float n110 = Dot(g110, x - 1, y - 1, z);
            float n111 = Dot(g111, x - 1, y - 1, z - 1);

            float nx00 = Lerp(n000, n100, u);
            float nx01 = Lerp(n001, n101, u);
            float nx10 = Lerp(n010, n110, u);
            float nx11 = Lerp(n011, n111, u);

            float nxy0 = Lerp(nx00, nx10, v);
            float nxy1 = Lerp(nx01, nx11, v);

            return Lerp(nxy0, nxy1, w);
        }

        private float Fade(float t)
        {
            return t * t * t * (t * (t * 6 - 15) + 10);
        }

        private float Dot(Vertex3f g, float x, float y, float z)
        {
            return g.x * x + g.y * y + g.z * z;
        }

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

            LoadSunColor(new Vertex3f(1.0f, 0.95f, 0.8f));
            LoadNearPlane(0.1f);
            LoadFarPlane(10000.0f);
            LoadCloudLayerBottom(_cloudLayerBottom);
            LoadCloudLayerTop(_cloudLayerTop);
            LoadCloudCoverage(0.5f);
            LoadCloudDensity(0.5f);
            LoadCloudSharpness(3.0f);
            LoadCloudDetailStrength(0.7f);
            LoadCloudOffset(new Vertex3f(0.0f, 0.0f, 0.0f));
            LoadTime(0.0f);
            LoadPrimaryStepCount(_primaryStepCount);
            LoadLightStepCount(_lightStepCount);
            LoadPrimaryStepSize(_primaryStepSize);
            LoadLightStepSize(_lightStepSize);

            Unbind();
        }

        /// <summary>
        /// 노이즈 텍스처를 유니폼으로 설정
        /// </summary>
        private void SetNoiseTextures()
        {
            Gl.ActiveTexture(TextureUnit.Texture0 + 2);
            Gl.BindTexture(TextureTarget.Texture3d, _noiseTexture);
            Gl.Uniform1(loc_noiseTexture, 2);

            Gl.ActiveTexture(TextureUnit.Texture0 + 3);
            Gl.BindTexture(TextureTarget.Texture3d, _detailNoiseTexture);
            Gl.Uniform1(loc_detailNoiseTexture, 3);

            Gl.ActiveTexture(TextureUnit.Texture0);
        }

        // === Load 메서드들 ===

        public void LoadSunDirection(Vertex3f direction)
        {
            Gl.Uniform3(loc_sunDirection, direction.x, direction.y, direction.z);
        }

        public void LoadSunColor(Vertex3f color)
        {
            Gl.Uniform3(loc_sunColor, color.x, color.y, color.z);
        }

        public void LoadCameraPosition(Vertex3f position)
        {
            Gl.Uniform3(loc_cameraPosition, position.x, position.y, position.z);
        }

        public void LoadNearPlane(float near)
        {
            Gl.Uniform1(loc_nearPlane, near);
        }

        public void LoadFarPlane(float far)
        {
            Gl.Uniform1(loc_farPlane, far);
        }

        public void LoadCloudLayerBottom(float bottom)
        {
            Gl.Uniform1(loc_cloudLayerBottom, bottom);
        }

        public void LoadCloudLayerTop(float top)
        {
            Gl.Uniform1(loc_cloudLayerTop, top);
        }

        public void LoadCloudCoverage(float coverage)
        {
            Gl.Uniform1(loc_cloudCoverage, coverage);
        }

        public void LoadCloudDensity(float density)
        {
            Gl.Uniform1(loc_cloudDensity, density);
        }

        public void LoadCloudSharpness(float sharpness)
        {
            Gl.Uniform1(loc_cloudSharpness, sharpness);
        }

        public void LoadCloudDetailStrength(float strength)
        {
            Gl.Uniform1(loc_cloudDetailStrength, strength);
        }

        public void LoadCloudOffset(Vertex3f offset)
        {
            Gl.Uniform3(loc_cloudOffset, offset.x, offset.y, offset.z);
        }

        public void LoadTime(float time)
        {
            Gl.Uniform1(loc_time, time);
        }

        public void LoadPrimaryStepCount(int count)
        {
            Gl.Uniform1(loc_primaryStepCount, count);
        }

        public void LoadLightStepCount(int count)
        {
            Gl.Uniform1(loc_lightStepCount, count);
        }

        public void LoadPrimaryStepSize(float size)
        {
            Gl.Uniform1(loc_primaryStepSize, size);
        }

        public void LoadLightStepSize(float size)
        {
            Gl.Uniform1(loc_lightStepSize, size);
        }

        /// <summary>
        /// 구름 텍스처 렌더링
        /// </summary>
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
            if (cloudOffset == null)
                cloudOffset = new Vertex3f(0.0f, 0.0f, 0.0f);

            Bind();

            LoadSunDirection(sunDirection);
            LoadCameraPosition(cameraPosition);
            LoadCloudCoverage(cloudCoverage);
            LoadCloudDensity(cloudDensity);
            LoadCloudSharpness(cloudSharpness);
            LoadCloudDetailStrength(cloudDetailStrength);
            LoadCloudOffset((Vertex3f)cloudOffset);
            LoadTime(0.0f);

            SetNoiseTextures();

            Gl.BindImageTexture(0, skyTextureId, 0, false, 0, BufferAccess.ReadOnly, InternalFormat.Rgba16f);
            Gl.BindImageTexture(1, cloudTextureId, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);

            uint groupsX = (uint)(_texWidth + 15) / 16;
            uint groupsY = (uint)(_texHeight + 15) / 16;
            Gl.DispatchCompute(groupsX, groupsY, 1);

            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

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
            LoadCloudCoverage(0.45f);
            LoadCloudDensity(0.6f);
            LoadCloudSharpness(4.0f);
            LoadCloudDetailStrength(0.8f);
            Unbind();
        }

        /// <summary>
        /// 적란운 구름 설정 (뇌우 구름)
        /// </summary>
        public void SetCumulonimbusCloud()
        {
            Bind();
            LoadCloudCoverage(0.7f);
            LoadCloudDensity(0.9f);
            LoadCloudSharpness(2.5f);
            LoadCloudDetailStrength(0.9f);
            Unbind();
        }

        /// <summary>
        /// 층운 구름 설정 (낮은 회색 구름층)
        /// </summary>
        public void SetStratusCloud()
        {
            Bind();
            LoadCloudCoverage(0.8f);
            LoadCloudDensity(0.5f);
            LoadCloudSharpness(1.0f);
            LoadCloudDetailStrength(0.4f);
            Unbind();
        }

        /// <summary>
        /// 층적운 구름 설정 (낮은 뭉게구름 층)
        /// </summary>
        public void SetStratocumulusCloud()
        {
            Bind();
            LoadCloudCoverage(0.6f);
            LoadCloudDensity(0.45f);
            LoadCloudSharpness(1.1f);
            LoadCloudDetailStrength(1.1f);
            Unbind();
        }

        /// <summary>
        /// 권운 구름 설정 (높은 깃털 모양 구름)
        /// </summary>
        public void SetCirrusCloud()
        {
            Bind();
            LoadCloudCoverage(0.3f);
            LoadCloudDensity(0.2f);
            LoadCloudSharpness(6.0f);
            LoadCloudDetailStrength(0.9f);
            Unbind();
        }

        /// <summary>
        /// 맑은 하늘 설정 (구름 거의 없음)
        /// </summary>
        public void SetClearSky()
        {
            Bind();
            LoadCloudCoverage(0.05f);
            LoadCloudDensity(0.2f);
            LoadCloudSharpness(5.0f);
            LoadCloudDetailStrength(0.6f);
            Unbind();
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public override void CleanUp()
        {
            base.CleanUp();

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
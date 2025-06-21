using Common;
using OpenGL;
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZetaExt;

namespace Shader
{
    /// <summary>
    /// 하늘과 구름 텍스처 생성을 위한 컴퓨트 셰이더
    /// </summary>
    public class SkyDomeTextureComputeShader : ShaderProgram<SkyDomeTextureComputeShader.UNIFORM_NAME>
    {
        public enum UNIFORM_NAME
        {
            // 컴퓨트 셰이더 유니폼
            sunPosition,        // 태양 위치 (정규화된 방향 벡터)
            cloudCoverage,      // 구름 양 (0.0 - 1.0)
            cloudBaseAltitude,  // 구름 바닥 고도 (0.0 - 1.0, 0 = 지평선, 1 = 천정)
            cloudTopAltitude,   // 구름 최상층 고도 (0.0 - 1.0)
            cloudFeatheringDistance, // 구름 경계면 페더링 거리 (0.0 - 0.2)

            cloudDensity,       // 구름 밀도 (0.0 - 2.0)
            cloudDetail,        // 구름 디테일 수준 (0.0 - 2.0)

            randomSeed,         // 랜덤 시드
            time,               // 시간 변수 (구름 애니메이션용)
            cloudOffset,        // 구름 오프셋 (태양 위치와 독립적인 구름 패턴용)

            // 총 유니폼 개수
            Count
        }

        // 컴퓨트 셰이더 파일 경로
        const string COMPUTE_FILE = @"\Shader\SkyDomeShader\skydome.comp";

        // 텍스처 속성
        private uint _skyTextureId;
        private readonly int _texWidth;
        private readonly int _texHeight;
        private Random _random = new Random();

        // 구름 패턴을 위한 고정된 오프셋 값
        private Vertex3f _fixedCloudOffset;

        // 텍스처 ID 속성
        public uint SkyTextureId { get => _skyTextureId; }

        // 텍스처 크기 속성
        public int TextureWidth { get => _texWidth; }
        public int TextureHeight { get => _texHeight; }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="projectPath">프로젝트 경로</param>
        /// <param name="width">텍스처 너비</param>
        /// <param name="height">텍스처 높이</param>
        public SkyDomeTextureComputeShader(string projectPath, int width = 512, int height = 512) : base()
        {
            _name = this.GetType().Name;
            _texWidth = width;
            _texHeight = height;

            // 컴퓨트 셰이더 파일 경로 설정
            ComputeFileName = projectPath + COMPUTE_FILE;

            // 셰이더 초기화
            InitCompileShader();

            // 텍스처 초기화
            InitializeTexture();

            // 고정된 구름 오프셋 초기화 (한 번만 생성하여 모든 렌더링에 사용)
            _fixedCloudOffset = new Vertex3f(
                (float)_random.NextDouble() * 100.0f - 50.0f,
                (float)_random.NextDouble() * 10.0f,
                (float)_random.NextDouble() * 100.0f - 50.0f
            );

            _fixedCloudOffset = new Vertex3f(32.0f, 5.0f, 61.0f);
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
        /// 텍스처 초기화 메서드
        /// </summary>
        private void InitializeTexture()
        {
            // 텍스처 생성
            _skyTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _skyTextureId);

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

        /// <summary>
        /// 상반구에서 랜덤한 태양 위치를 생성합니다.
        /// </summary>
        /// <returns>태양의 방향 벡터 (정규화됨)</returns>
        public Vertex3f GenerateRandomSunPosition()
        {
            // 태양의 고도각 - 수평선 위 -10~40도 (최상단 90도 제외)
            double sunAltitude = (-10.0f + _random.NextDouble() * 50.0) * Math.PI / 180.0;

            // 태양의 방위각 - 0-360도
            double sunAzimuth = _random.NextDouble() * 2.0 * Math.PI;

            // 태양 위치를 카테시안 좌표로 변환
            float sunX = (float)(Math.Cos(sunAltitude) * Math.Cos(sunAzimuth));
            float sunY = (float)(Math.Cos(sunAltitude) * Math.Sin(sunAzimuth));
            float sunZ = (float)(Math.Sin(sunAltitude));  // z축이 상향

            // 태양 위치 정규화
            Vertex3f sunPosition = new Vertex3f(sunX, sunY, sunZ).Normalized;
            Console.WriteLine($"랜덤 태양 위치: X={sunPosition.x}, Y={sunPosition.y}, Z={sunPosition.z}");

            return sunPosition;
        }

        /// <summary>
        /// 하늘 텍스처 생성 메서드
        /// </summary>
        /// <param name="cloudCoverage">구름의 양 (0.0 = 맑음, 1.0 = 흐림)</param>
        /// <param name="cloudAltitude">구름의 고도 (0.0 = 지평선에 가까움, 0.8 = 천정에 가까움)</param>
        /// <param name="cloudThickness">구름의 두께 (0.1 = 얇음, 1.0 = 두꺼움)</param>
        /// <returns>생성된 텍스처 ID</returns>
        public uint GenerateSkyTexture(float cloudCoverage = 0.5f, float cloudAltitude = 0.2f, float cloudThickness = 0.5f)
        {
            // 매개변수 범위 확인
            cloudCoverage = cloudCoverage.Clamp(0.0f, 1.0f);
            cloudAltitude = cloudAltitude.Clamp(0.0f, 0.8f);
            cloudThickness = cloudThickness.Clamp(0.1f, 1.0f);

            // 랜덤 태양 위치 생성
            Vertex3f sunPosition = GenerateRandomSunPosition();

            // 컴퓨트 셰이더 실행
            GenerateSkyTextureWithSunPosition(sunPosition, cloudCoverage, cloudAltitude, cloudThickness);

            // 텍스처 ID 반환
            return _skyTextureId;
        }

        /// <summary>
        /// 지정된 태양 위치로 하늘 텍스처 생성 메서드
        /// </summary>
        /// <param name="sunPosition">태양 위치 벡터</param>
        /// <param name="cloudCoverage">구름의 양 (0.0 = 맑음, 1.0 = 흐림)</param>
        /// <param name="cloudAltitude">구름의 고도 (0.0 = 지평선에 가까움, 0.8 = 천정에 가까움)</param>
        /// <param name="cloudThickness">구름의 두께 (0.1 = 얇음, 1.0 = 두꺼움)</param>
        /// <param name="useFixedCloudPattern">고정된 구름 패턴 사용 여부 (태양 위치와 독립적)</param>
        /// <returns>생성된 텍스처 ID</returns>
        public uint GenerateSkyTextureWithSunPosition(
            Vertex3f sunPosition,
            float cloudCoverage = 0.5f,
            float cloudAltitude = 0.2f,
            float cloudThickness = 0.5f,
            bool useFixedCloudPattern = true)
        {
            // 매개변수 범위 확인
            cloudCoverage = cloudCoverage.Clamp(0.0f, 1.0f);
            cloudAltitude = cloudAltitude.Clamp(0.0f, 0.8f);
            cloudThickness = cloudThickness.Clamp(0.1f, 1.0f);

            // 컴퓨트 셰이더 바인딩
            Bind();

            // 유니폼 변수 설정
            LoadUniform(UNIFORM_NAME.sunPosition, sunPosition);

            // 개선된 구름 레이어 파라미터
            LoadUniform(UNIFORM_NAME.cloudCoverage, 0.95f);       // 구름 커버리지 
            LoadUniform(UNIFORM_NAME.cloudBaseAltitude, 0.01f);    // 구름 바닥 고도 (정규화된 0-1 높이)
            LoadUniform(UNIFORM_NAME.cloudTopAltitude, 0.25f);    // 구름 최상층 고도
            LoadUniform(UNIFORM_NAME.cloudFeatheringDistance, 0.1f); // 경계면 페더링 거리

            // 구름 품질 매개변수
            LoadUniform(UNIFORM_NAME.cloudDensity, 2.99f);         // 구름 밀도
            LoadUniform(UNIFORM_NAME.cloudDetail, 1.2f);          // 구름 디테일 수준

            // 시간 값 설정 (여기서는 0으로 고정하여 구름 패턴을 고정)
            LoadUniform(UNIFORM_NAME.time, 0.0f);

            // 구름 오프셋 설정 (고정된 패턴 사용)
            if (useFixedCloudPattern)
            {
                // 태양 위치에 관계없이 항상 같은 구름 패턴 사용
                LoadUniform(UNIFORM_NAME.cloudOffset, _fixedCloudOffset);
            }
            else
            {
                // 태양 위치에 따른 다른 구름 패턴 사용 (기존 방식)
                LoadUniform(UNIFORM_NAME.cloudOffset, new Vertex3f(0.0f, 0.0f, 0.0f));
            }

            // 랜덤 시드 설정 (여기서도 고정된 값 사용)
            Vertex4f randomSeed = new Vertex4f(
                42.0f,  // 고정된 시드 값
                23.0f,
                76.0f,
                15.0f
            );
            LoadUniform(UNIFORM_NAME.randomSeed, randomSeed);

            // 이미지 바인딩
            Gl.BindImageTexture(0, _skyTextureId, 0, false, 0, BufferAccess.WriteOnly, InternalFormat.Rgba16f);

            // 계산 셰이더 디스패치
            Gl.DispatchCompute((uint)(_texWidth / 16) + 1, (uint)(_texHeight / 16) + 1, 1);

            // 메모리 배리어 (계산 완료 대기)
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

            // 언바인딩
            Unbind();

            // 텍스처 반환
            return _skyTextureId;
        }

        /// <summary>
        /// 텍스처를 비트맵으로 저장하는 메서드
        /// </summary>
        /// <param name="filename">저장할 파일 이름</param>
        public void SaveTextureToBitmap(string filename)
        {
            // 픽셀 데이터를 저장할 배열
            float[] pixelData = new float[_texWidth * _texHeight * 4]; // RGBA

            // 텍스처 바인딩
            Gl.BindTexture(TextureTarget.Texture2d, _skyTextureId);

            // 픽셀 데이터 가져오기
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Rgba, PixelType.Float, pixelData);

            // 텍스처 언바인딩
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // RGBA 데이터를 Bitmap으로 변환
            using (Bitmap bitmap = new Bitmap(_texWidth, _texHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                // 비트맵 데이터 잠금
                BitmapData bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, _texWidth, _texHeight),
                    ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                // 픽셀 데이터 변환 및 복사
                byte[] bgraData = new byte[_texWidth * _texHeight * 4];

                for (int i = 0; i < _texWidth * _texHeight; i++)
                {
                    // float RGBA를 byte BGRA로 변환 (톤 매핑 적용)
                    int baseIdx = i * 4;

                    // 간단한 톤 매핑 (HDR -> LDR)
                    float r = pixelData[baseIdx + 0].Clamp(0.0f, 1.0f);
                    float g = pixelData[baseIdx + 1].Clamp(0.0f, 1.0f);
                    float b = pixelData[baseIdx + 2].Clamp(0.0f, 1.0f);
                    float a = pixelData[baseIdx + 3].Clamp(0.0f, 1.0f);

                    // byte로 변환 (BGRA 순서)
                    bgraData[baseIdx + 0] = (byte)(b * 255);
                    bgraData[baseIdx + 1] = (byte)(g * 255);
                    bgraData[baseIdx + 2] = (byte)(r * 255);
                    bgraData[baseIdx + 3] = (byte)(a * 255);
                }

                // 비트맵에 데이터 복사
                Marshal.Copy(bgraData, 0, bitmapData.Scan0, bgraData.Length);

                // 비트맵 잠금 해제
                bitmap.UnlockBits(bitmapData);

                // 비트맵 저장
                bitmap.Save(filename, ImageFormat.Png);

                Console.WriteLine($"텍스처가 {filename}에 저장되었습니다.");
            }
        }

        /// <summary>
        /// 메모리 해제 메서드
        /// </summary>
        public override void CleanUp()
        {
            base.CleanUp();

            // 텍스처 삭제
            Gl.DeleteTextures(_skyTextureId);
        }
    }
}
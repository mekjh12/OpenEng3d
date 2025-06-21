using System;
using Common;
using OpenGL;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Common.Abstractions;

namespace Shader
{
    /// <summary>
    /// 하늘과 구름 렌더링을 관리하는 스카이돔 셰이더 클래스
    /// </summary>
    public class SkyDomeTexture2dShader
    {
        // 하늘색 및 구름 컴퓨트 셰이더
        private SkyColorShader _skyColorShader;
        private VolumetricCloudShader _cloudShader;

        // 텍스처
        private uint _skyTexture;       // 하늘색만 포함된 텍스처
        private uint _finalTexture;     // 하늘색과 구름이 합쳐진 최종 텍스처

        // 텍스처 크기
        private int _width, _height;

        // 랜덤 생성기
        private Random _random = new Random();

        // 고정된 구름 오프셋
        private Vertex3f _fixedCloudOffset;

        /// <summary>
        /// 하늘색만 포함된 텍스처 ID 반환
        /// </summary>
        public uint SkyTextureId => _skyTexture;

        /// <summary>
        /// 최종 하늘 텍스처(구름 포함) ID 반환
        /// </summary>
        public uint FinalTexture => _finalTexture;

        /// <summary>
        /// 하늘색 셰이더 객체 반환
        /// </summary>
        public SkyColorShader SkyColorShader => _skyColorShader;

        /// <summary>
        /// 스카이돔 셰이더 초기화
        /// </summary>
        /// <param name="projectPath">프로젝트 경로</param>
        /// <param name="width">텍스처 너비(픽셀)</param>
        /// <param name="height">텍스처 높이(픽셀)</param>
        public SkyDomeTexture2dShader(string projectPath, int width = 1024, int height = 256)
        {
            _width = width;
            _height = height;

            // 하늘색 컴퓨트 셰이더 초기화
            _skyColorShader = new SkyColorShader(projectPath, width, height);

            // 구름 컴퓨트 셰이더 초기화
            _cloudShader = new VolumetricCloudShader(projectPath, width, height);

            // 텍스처 초기화
            InitializeTextures();

            // 고정된 구름 오프셋 초기화 (한 번만 생성하여 모든 렌더링에 사용)
            _fixedCloudOffset = new Vertex3f(
                (float)_random.NextDouble() * 100.0f - 50.0f,
                (float)_random.NextDouble() * 10.0f,
                (float)_random.NextDouble() * 100.0f - 50.0f
            );

            // 테스트용 고정 값
            _fixedCloudOffset = new Vertex3f(32.0f, 5.0f, 61.0f);
        }

        /// <summary>
        /// 텍스처 초기화 메서드
        /// </summary>
        private void InitializeTextures()
        {
            // 하늘색 텍스처 생성
            _skyTexture = CreateTexture(_width, _height);

            // 최종 텍스처 생성
            _finalTexture = CreateTexture(_width, _height);
        }

        /// <summary>
        /// RGBA16F 포맷의 텍스처 생성
        /// </summary>
        private uint CreateTexture(int width, int height)
        {
            uint textureID;
            textureID = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, textureID);

            // RGBA16F 포맷으로 텍스처 생성 (HDR 지원)
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, width, height, 0,
                        OpenGL.PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);

            // 텍스처 파라미터 설정
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, TextureMinFilter.Linear);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, TextureMagFilter.Linear);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, TextureWrapMode.ClampToEdge);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, TextureWrapMode.ClampToEdge);

            // 텍스처 바인딩 해제
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            return textureID;
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
        /// <param name="cloudBaseAltitude">구름 바닥 고도 (0.0-1.0)</param>
        /// <param name="cloudTopAltitude">구름 상단 고도 (0.0-1.0)</param>
        /// <returns>생성된 텍스처 ID</returns>
        public uint GenerateSkyTexture(
            Vertex3f cameraPosition,
            float cloudCoverage = 0.5f,
            float cloudBaseAltitude = 0.1f,
            float cloudTopAltitude = 0.3f)
        {
            // 매개변수 범위 확인
            cloudCoverage = ClampValue(cloudCoverage, 0.0f, 1.0f);
            cloudBaseAltitude = ClampValue(cloudBaseAltitude, 0.0f, 0.8f);
            cloudTopAltitude = ClampValue(cloudTopAltitude, cloudBaseAltitude + 0.1f, 0.9f);

            // 랜덤 태양 위치 생성
            Vertex3f sunPosition = GenerateRandomSunPosition();

            // 지정된 태양 위치로 하늘 렌더링
            return GenerateSkyTextureWithSunPosition(
                sunPosition,
                cameraPosition,
                cloudCoverage,
                cloudBaseAltitude,
                cloudTopAltitude);
        }

        /// <summary>
        /// 지정된 태양 위치로 하늘 텍스처 생성 메서드
        /// </summary>
        /// <param name="sunPosition">태양 위치 벡터</param>
        /// <param name="cloudCoverage">구름의 양 (0.0 = 맑음, 1.0 = 흐림)</param>
        /// <param name="cloudBaseAltitude">구름 바닥 고도 (0.0-1.0)</param>
        /// <param name="cloudTopAltitude">구름 상단 고도 (0.0-1.0)</param>
        /// <param name="useFixedCloudPattern">고정된 구름 패턴 사용 여부 (태양 위치와 독립적)</param>
        /// <returns>생성된 텍스처 ID</returns>
        public uint GenerateSkyTextureWithSunPosition(
            Vertex3f sunPosition,
            Vertex3f cameraPosition,
            float cloudCoverage = 0.99f,
            float cloudBaseAltitude = 0.1f,
            float cloudTopAltitude = 0.9f,
            bool useFixedCloudPattern = true)
        {
            // 매개변수 범위 확인
            cloudCoverage = ClampValue(cloudCoverage, 0.0f, 1.0f);
            cloudBaseAltitude = ClampValue(cloudBaseAltitude, 0.0f, 1.0f);
            cloudTopAltitude = ClampValue(cloudTopAltitude, 0.0f, 1.0f);

            // 1단계: 하늘색 렌더링
            _skyColorShader.RenderSkyTexture(_skyTexture, sunPosition);

            // 2단계: 구름 렌더링
            _cloudShader.RenderCloudTexture(
                _skyTexture,
                _finalTexture,
                sunPosition,
                cameraPosition,
                cloudCoverage,
                cloudBaseAltitude,
                cloudTopAltitude);

            // 최종 텍스처 ID 반환
            return _finalTexture;
        }

        /// <summary>
        /// 텍스처를 비트맵으로 저장하는 메서드
        /// </summary>
        /// <param name="filename">저장할 파일 이름</param>
        public void SaveTextureToBitmap(string filename)
        {
            // 픽셀 데이터를 저장할 배열
            float[] pixelData = new float[_width * _height * 4]; // RGBA

            // 텍스처 바인딩
            Gl.BindTexture(TextureTarget.Texture2d, _finalTexture);

            // 픽셀 데이터 가져오기
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Rgba, PixelType.Float, pixelData);

            // 텍스처 언바인딩
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // RGBA 데이터를 Bitmap으로 변환
            using (Bitmap bitmap = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                // 비트맵 데이터 잠금
                BitmapData bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, _width, _height),
                    ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                // 픽셀 데이터 변환 및 복사
                byte[] bgraData = new byte[_width * _height * 4];

                for (int i = 0; i < _width * _height; i++)
                {
                    // float RGBA를 byte BGRA로 변환 (톤 매핑 적용)
                    int baseIdx = i * 4;

                    // 간단한 톤 매핑 (HDR -> LDR)
                    float r = ClampValue(pixelData[baseIdx + 0], 0.0f, 1.0f);
                    float g = ClampValue(pixelData[baseIdx + 1], 0.0f, 1.0f);
                    float b = ClampValue(pixelData[baseIdx + 2], 0.0f, 1.0f);
                    float a = ClampValue(pixelData[baseIdx + 3], 0.0f, 1.0f);

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
        /// 값을 지정된 범위로 제한
        /// </summary>
        private float ClampValue(float value, float min, float max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Dispose()
        {
            if (_skyColorShader != null)
            {
                //_skyColorShader.Dispose();
                _skyColorShader = null;
            }

            if (_cloudShader != null)
            {
                //_cloudShader.Dispose();
                _cloudShader = null;
            }

            if (_skyTexture != 0)
            {
                Gl.DeleteTextures(new uint[] { _skyTexture });
                _skyTexture = 0;
            }

            if (_finalTexture != 0)
            {
                Gl.DeleteTextures(new uint[] { _finalTexture });
                _finalTexture = 0;
            }
        }
    }
}
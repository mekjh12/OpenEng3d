using Common.Abstractions;
using OpenGL;
using Shader;
using System;
using System.Drawing.Imaging;
using System.Drawing;

namespace Renderer
{
    /// <summary>
    /// [실패]
    /// Water Flow Accumulation 시뮬레이터
    /// Compute Shader를 이용한 계곡/강 감지
    /// </summary>
    public class WaterFlowSimulator
    {
        private WaterFlowComputeShader _shader;
        private GaussianBlurComputeShader _blurShader;

        private Texture _heightMapTexture;
        private Texture _blurredHeightMapTexture;

        // Ping-Pong 버퍼
        private uint _waterBuffer0;
        private uint _waterBuffer1;

        private int _width;
        private int _height;

        /// <summary>
        /// 생성자
        /// </summary>
        public WaterFlowSimulator(string projectPath, Texture heightmap)
        {
            _heightMapTexture = heightmap;

            // 텍스처 크기 가져오기
            Gl.BindTexture(TextureTarget.Texture2d, heightmap.TextureID);
            Gl.GetTexLevelParameter(TextureTarget.Texture2d, 0,
                GetTextureParameter.TextureWidth, out _width);
            Gl.GetTexLevelParameter(TextureTarget.Texture2d, 0,
                GetTextureParameter.TextureHeight, out _height);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            Console.WriteLine($"[WaterFlow] Heightmap 크기: {_width} x {_height}");

            // Shader 로드
            _shader = new WaterFlowComputeShader(projectPath);
            _blurShader = new GaussianBlurComputeShader(projectPath);

            // 물 버퍼 생성
            _waterBuffer0 = CreateWaterBuffer();
            _waterBuffer1 = CreateWaterBuffer();
            ClearWaterBuffer(_waterBuffer0);
        }

        /// <summary>
        /// R32F 높이맵을 PNG로 저장 (정규화 방식 통일)
        /// </summary>
        public void SaveHeightmapToPNG(Texture heightmapTexture, string outputPath,
            float? forceMin = null, float? forceMax = null)  // ⭐ 강제 범위 옵션
        {
            Console.WriteLine($"[WaterFlow] Heightmap 저장 중: {outputPath}");

            // R32F 포맷으로 읽기
            float[] heightData = new float[_width * _height];

            Gl.BindTexture(TextureTarget.Texture2d, heightmapTexture.TextureID);
            Gl.GetTexImage(
                TextureTarget.Texture2d,
                0,
                OpenGL.PixelFormat.Red,
                PixelType.Float,
                heightData
            );
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // ⭐ 강제 범위가 없으면 자동 계산
            float minHeight = forceMin ?? float.MaxValue;
            float maxHeight = forceMax ?? float.MinValue;

            if (!forceMin.HasValue || !forceMax.HasValue)
            {
                for (int i = 0; i < heightData.Length; i++)
                {
                    float h = heightData[i];

                    if (float.IsNaN(h) || float.IsInfinity(h))
                        continue;

                    if (h < minHeight) minHeight = h;
                    if (h > maxHeight) maxHeight = h;
                }
            }

            Console.WriteLine($"  최소 높이: {minHeight:F6}");
            Console.WriteLine($"  최대 높이: {maxHeight:F6}");
            Console.WriteLine($"  높이 범위: {(maxHeight - minHeight):F6}");

            float range = maxHeight - minHeight;
            if (range < 0.000001f)
            {
                Console.WriteLine("  ⚠️ 경고: 높이 범위가 거의 0입니다!");
                range = 1.0f;
            }

            // Bitmap 생성
            using (Bitmap bmp = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0, _width, _height),
                    ImageLockMode.WriteOnly,
                    bmp.PixelFormat
                );

                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;

                    for (int y = 0; y < _height; y++)
                    {
                        for (int x = 0; x < _width; x++)
                        {
                            int srcIndex = ((_height - 1 - y) * _width + x);  // Y 플립
                            int dstIndex = (y * _width + x) * 4;

                            float height = heightData[srcIndex];

                            // NaN/Inf 체크
                            if (float.IsNaN(height) || float.IsInfinity(height))
                                height = minHeight;

                            // ⭐ 정규화: 낮은 곳=0(검정), 높은 곳=255(흰색)
                            float normalized = (height - minHeight) / range;
                            normalized = Math.Max(0f, Math.Min(1f, normalized));
                            byte value = (byte)(normalized * 255);

                            // 그레이스케일
                            ptr[dstIndex + 0] = value;  // B
                            ptr[dstIndex + 1] = value;  // G
                            ptr[dstIndex + 2] = value;  // R
                            ptr[dstIndex + 3] = 255;    // A
                        }
                    }
                }

                bmp.UnlockBits(bmpData);
                bmp.Save(outputPath, ImageFormat.Png);
            }

            Console.WriteLine($"[WaterFlow] Heightmap 저장 완료: {outputPath}");
        }

        private Texture ApplyGaussianBlur(Texture inputHeightmap, float blurStrength = 0.3f)
        {
            try
            {
                Console.WriteLine($"[WaterFlow] 블러 적용 시작... 강도: {blurStrength:F2}");

                // ⭐ 1단계: 출력 텍스처 생성 및 초기화
                uint blurredID = CreateR32FTexture(_width, _height);

                // ⭐ 2단계: Shader 바인드 및 Uniform 설정
                _blurShader.Bind();

                // ⭐ 3단계: 이미지 텍스처 바인딩 (원래대로 BufferAccess 사용)
                Gl.BindImageTexture(
                    0,
                    inputHeightmap.TextureID,
                    0, false, 0,
                    BufferAccess.ReadOnly,  // ⭐ 원래대로
                    InternalFormat.R32f
                );

                Gl.BindImageTexture(
                    1,
                    blurredID,
                    0, false, 0,
                    BufferAccess.WriteOnly,  // ⭐ 원래대로
                    InternalFormat.R32f
                );

                // ⭐ 4단계: Dispatch
                int groupsX = (_width + 15) / 16;
                int groupsY = (_height + 15) / 16;

                Console.WriteLine($"  Dispatching: {groupsX} x {groupsY} workgroups");

                Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

                _blurShader.Unbind();

                // ⭐ 5단계: 결과 검증
                float[] testData = new float[100];
                Gl.BindTexture(TextureTarget.Texture2d, blurredID);
                Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Red, PixelType.Float, testData);
                Gl.BindTexture(TextureTarget.Texture2d, 0);

                float min = float.MaxValue, max = float.MinValue;
                int badCount = 0;

                for (int i = 0; i < testData.Length; i++)
                {
                    float v = testData[i];
                    if (float.IsNaN(v) || float.IsInfinity(v))
                    {
                        badCount++;
                        continue;
                    }
                    if (v < min) min = v;
                    if (v > max) max = v;
                }

                Console.WriteLine($"  결과 검증: Min={min:F4}, Max={max:F4}, Bad={badCount}/100");

                if (badCount > 10 || min < 0 || max > 1)
                {
                    Console.WriteLine("  ❌ 블러 결과가 이상합니다. 원본 사용.");
                    Gl.DeleteTextures(blurredID);
                    return inputHeightmap;
                }

                Console.WriteLine("  ✅ 블러 성공!");
                return new Texture(blurredID, _width, _height);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ 블러 중 예외 발생: {ex.Message}");
                Console.WriteLine($"  스택: {ex.StackTrace}");
                return inputHeightmap;
            }
        }

        /// <summary>
        /// R32F 포맷 텍스처 생성 헬퍼
        /// </summary>
        private uint CreateR32FTexture(int w, int h)
        {
            uint texID = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, texID);

            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.R32f,
                w, h, 0,
                OpenGL.PixelFormat.Red,
                PixelType.Float,
                IntPtr.Zero
            );

            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMagFilter, Gl.LINEAR);

            Gl.BindTexture(TextureTarget.Texture2d, 0);
            return texID;
        }

        /// <summary>
        /// R32F 포맷 텍스처 생성
        /// </summary>
        private uint CreateWaterBuffer()
        {
            uint buffer = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, buffer);

            // ⭐ R32f -> Rg32f 로 변경 (채널 2개 사용)
            Gl.TexImage2D(
                TextureTarget.Texture2d, 0, InternalFormat.Rg32f, // 여기 변경
                _width, _height, 0,
                OpenGL.PixelFormat.Rg, // 여기 변경
                PixelType.Float, IntPtr.Zero
            );

            // 텍스처 파라미터 설정
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMagFilter, Gl.LINEAR);

            Gl.BindTexture(TextureTarget.Texture2d, 0);
            return buffer;
        }

        /// <summary>
        /// 물 버퍼를 0으로 초기화
        /// </summary>
        private void ClearWaterBuffer(uint buffer)
        {
            // ⭐ RG32F이므로 2배 크기
            float[] zeros = new float[_width * _height * 2];
            for (int i = 0; i < zeros.Length; i++)
            {
                zeros[i] = 0f;
            }

            Gl.BindTexture(TextureTarget.Texture2d, buffer);
            Gl.TexSubImage2D(  // ⭐ TexImage2D → TexSubImage2D
                TextureTarget.Texture2d,
                0, 0, 0,
                _width,
                _height,
                OpenGL.PixelFormat.Rg,  // ⭐ Red → Rg
                PixelType.Float,
                zeros
            );
            Gl.BindTexture(TextureTarget.Texture2d, 0);
        }

        /// <summary>
        /// N 프레임 시뮬레이션 실행 (물 뿌리기 + 흐르기 단계 분리)
        /// </summary>
        /// <param name="rainIterations">물을 뿌리는 횟수</param>
        /// <param name="flowIterations">물이 흐르는 횟수 (물 추가 없음)</param>
        /// <param name="deltaWater">매 프레임 추가할 물의 양</param>
        /// <param name="flowRate">흐름 속도 (0.0~1.0)</param>
        /// <param name="evaporationRate">증발률 (0.0~1.0)</param>
        /// <summary>
        /// 시뮬레이션 실행 (블러된 높이맵 사용)
        /// </summary>
        public Texture Simulate(
            int rainIterations = 100,
            int flowIterations = 200,
            float deltaWater = 1.0f,
            float flowRate = 0.8f,
            float evaporationRate = 0.0f,
            float maxWaterDepth = 50.0f)  // ⭐ 추가
        {
            Console.WriteLine($"[WaterFlow] 시뮬레이션 시작");
            Console.WriteLine($"  물 뿌리기: {rainIterations}회");
            Console.WriteLine($"  물 흐르기: {flowIterations}회");
            Console.WriteLine($"  최대 수심 제한: {maxWaterDepth:F1}");

            bool useBuffer0 = true;

            _shader.Bind();

            // Phase 1: Rain
            Console.WriteLine("[Phase 1] 물 뿌리기 시작...");
            //_shader.LoadSimulationParams(deltaWater, flowRate, evaporationRate, maxWaterDepth);

            for (int i = 0; i < rainIterations; i++)
            {
                uint readBuffer = useBuffer0 ? _waterBuffer0 : _waterBuffer1;
                uint writeBuffer = useBuffer0 ? _waterBuffer1 : _waterBuffer0;

                BindBuffers(_heightMapTexture.TextureID, readBuffer, writeBuffer);
                _shader.Dispatch(_width, _height);
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

                useBuffer0 = !useBuffer0;

                if ((i + 1) % 100 == 0)
                    Console.WriteLine($"  비 내림 [{i + 1}/{rainIterations}]");
            }

            // Phase 2: Flow
            if (flowIterations > 0)
            {
                Console.WriteLine("[Phase 2] 물 흐르기 시작...");
                //_shader.LoadSimulationParams(0.0f, flowRate, evaporationRate, maxWaterDepth);

                for (int i = 0; i < flowIterations; i++)
                {
                    uint readBuffer = useBuffer0 ? _waterBuffer0 : _waterBuffer1;
                    uint writeBuffer = useBuffer0 ? _waterBuffer1 : _waterBuffer0;

                    BindBuffers(_heightMapTexture.TextureID, readBuffer, writeBuffer);
                    _shader.Dispatch(_width, _height);
                    Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

                    useBuffer0 = !useBuffer0;

                    if ((i + 1) % 100 == 0)
                        Console.WriteLine($"  물 흐름 [{i + 1}/{flowIterations}]");
                }
            }

            _shader.Unbind();

            Console.WriteLine("[WaterFlow] 시뮬레이션 완료!");

            uint finalBuffer = useBuffer0 ? _waterBuffer0 : _waterBuffer1;
            return new Texture(finalBuffer, _width, _height);
        }

        /// <summary>
        /// 이미지 버퍼 바인딩 헬퍼 메서드
        /// </summary>
        private void BindBuffers(uint heightmapID, uint readBuffer, uint writeBuffer)
        {
            Gl.BindImageTexture(
                0,
                heightmapID,  // ⭐ 블러된 높이맵 사용
                0, false, 0,
                BufferAccess.ReadOnly,
                InternalFormat.R32f
            );

            Gl.BindImageTexture(
                1,
                readBuffer,
                0, false, 0,
                BufferAccess.ReadOnly,
                InternalFormat.Rg32f
            );

            Gl.BindImageTexture(
                2,
                writeBuffer,
                0, false, 0,
                BufferAccess.WriteOnly,
                InternalFormat.Rg32f
            );
        }

        /// <summary>
        /// 계곡 맵 생성 (물 축적량 → Valley Strength)
        /// </summary>
        /// <param name="waterAccumulation">물 축적 텍스처</param>
        /// <param name="thresholdPercentile">임계값 백분위수 (0.0~1.0, 예: 0.8 = 상위 20%만 계곡)</param>
        public Texture GenerateValleyMap(Texture waterAccumulation, float thresholdPercentile = 0.8f)
        {
            Console.WriteLine("[WaterFlow] Valley Map 생성 중...");

            // ⭐ RG32F 포맷으로 읽기
            float[] waterData = new float[_width * _height * 2];

            Gl.BindTexture(TextureTarget.Texture2d, waterAccumulation.TextureID);
            Gl.GetTexImage(
                TextureTarget.Texture2d,
                0,
                OpenGL.PixelFormat.Rg,  // ⭐ Red → Rg
                PixelType.Float,
                waterData
            );
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 수심만 추출 (R 채널)
            float[] depthOnly = new float[_width * _height];
            float maxWater = 0f;

            for (int i = 0; i < depthOnly.Length; i++)
            {
                float depth = waterData[i * 2];  // ⭐ R 채널만

                if (float.IsNaN(depth) || float.IsInfinity(depth))
                    depth = 0f;

                depthOnly[i] = depth;
                if (depth > maxWater) maxWater = depth;
            }

            Console.WriteLine($"  최대 물 축적량: {maxWater:F2}");

            if (maxWater < 0.001f)
            {
                Console.WriteLine("  ❌ 오류: 물이 전혀 축적되지 않았습니다!");
                return null;
            }

            // 백분위수 계산
            float[] sortedWater = (float[])depthOnly.Clone();
            Array.Sort(sortedWater);

            int thresholdIndex = (int)(sortedWater.Length * thresholdPercentile);
            float absoluteThreshold = sortedWater[thresholdIndex];

            Console.WriteLine($"  임계값 백분위수: {thresholdPercentile * 100:F1}%");
            Console.WriteLine($"  절대 임계값: {absoluteThreshold:F4}");

            // Valley Map 생성 (R32F)
            float[] valleyData = new float[_width * _height];
            int valleyPixelCount = 0;

            for (int i = 0; i < valleyData.Length; i++)
            {
                if (depthOnly[i] >= absoluteThreshold)
                {
                    valleyData[i] = depthOnly[i] / maxWater;  // 정규화
                    valleyPixelCount++;
                }
                else
                {
                    valleyData[i] = 0f;
                }
            }

            float valleyPercentage = (valleyPixelCount / (float)valleyData.Length) * 100f;
            Console.WriteLine($"  계곡으로 판단된 픽셀: {valleyPixelCount} ({valleyPercentage:F2}%)");

            // 텍스처 생성
            uint valleyTexID = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, valleyTexID);

            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.R32f,  // ⭐ Valley는 R32F (단일 채널)
                _width, _height, 0,
                OpenGL.PixelFormat.Red,
                PixelType.Float,
                valleyData
            );

            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d,
                TextureParameterName.TextureMagFilter, Gl.LINEAR);

            Gl.BindTexture(TextureTarget.Texture2d, 0);

            Console.WriteLine("[WaterFlow] Valley Map 생성 완료!");

            return new Texture(valleyTexID, _width, _height);
        }

        /// <summary>
        /// Water Map을 PNG로 저장 (RG32F 포맷 지원)
        /// R 채널: 수심(Depth)
        /// G 채널: 유량(Flux)
        /// </summary>
        public void SaveWaterMapToPNG(Texture waterTexture, string outputPath)
        {
            Console.WriteLine($"[WaterFlow] Water Map 저장 중: {outputPath}");

            // ⭐ RG32F 포맷으로 읽기 (2채널)
            float[] waterData = new float[_width * _height * 2];

            Gl.BindTexture(TextureTarget.Texture2d, waterTexture.TextureID);
            Gl.GetTexImage(
                TextureTarget.Texture2d,
                0,
                OpenGL.PixelFormat.Rg,  // ⭐ Red → Rg
                PixelType.Float,
                waterData
            );
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 통계 계산 (R 채널만 = 수심)
            float maxWater = 0f;
            float minWater = float.MaxValue;

            for (int i = 0; i < waterData.Length; i += 2)  // ⭐ 2칸씩 점프 (R, G, R, G...)
            {
                float depth = waterData[i];  // R 채널

                if (float.IsNaN(depth) || float.IsInfinity(depth))
                    continue;

                if (depth > maxWater) maxWater = depth;
                if (depth < minWater) minWater = depth;
            }

            Console.WriteLine($"  최소 수심: {minWater:F4}");
            Console.WriteLine($"  최대 수심: {maxWater:F4}");

            if (maxWater < 0.0001f)
            {
                Console.WriteLine("  ⚠️ 경고: 모든 픽셀의 물이 거의 0입니다!");
                maxWater = 1.0f; // 0 방지
            }

            // Bitmap 생성
            using (Bitmap bmp = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0, _width, _height),
                    ImageLockMode.WriteOnly,
                    bmp.PixelFormat
                );

                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;

                    for (int y = 0; y < _height; y++)
                    {
                        for (int x = 0; x < _width; x++)
                        {
                            int srcIndex = ((_height - 1 - y) * _width + x) * 2;  // ⭐ *2 (RG)
                            int dstIndex = (y * _width + x) * 4;

                            float depth = waterData[srcIndex];      // R 채널
                            float flux = waterData[srcIndex + 1];   // G 채널

                            // NaN/Inf 체크
                            if (float.IsNaN(depth) || float.IsInfinity(depth))
                                depth = 0f;

                            // 정규화 (0~255)
                            float normalized = depth / maxWater;
                            normalized = Math.Max(0f, Math.Min(1f, normalized));
                            byte value = (byte)(normalized * 255);

                            // 그레이스케일
                            ptr[dstIndex + 0] = value;  // B
                            ptr[dstIndex + 1] = value;  // G
                            ptr[dstIndex + 2] = value;  // R
                            ptr[dstIndex + 3] = 255;    // A
                        }
                    }
                }

                bmp.UnlockBits(bmpData);
                bmp.Save(outputPath, ImageFormat.Png);
            }

            Console.WriteLine($"[WaterFlow] Water Map 저장 완료: {outputPath}");
        }

        /// <summary>
        /// ⭐ 추가: 유량(Flux) 맵 저장
        /// </summary>
        public void SaveFluxMapToPNG(Texture waterTexture, string outputPath)
        {
            Console.WriteLine($"[WaterFlow] Flux Map 저장 중: {outputPath}");

            // RG32F 포맷으로 읽기
            float[] waterData = new float[_width * _height * 2];

            Gl.BindTexture(TextureTarget.Texture2d, waterTexture.TextureID);
            Gl.GetTexImage(
                TextureTarget.Texture2d,
                0,
                OpenGL.PixelFormat.Rg,
                PixelType.Float,
                waterData
            );
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 통계 계산 (G 채널 = 유량)
            float maxFlux = 0f;

            for (int i = 1; i < waterData.Length; i += 2)  // G 채널만
            {
                float flux = waterData[i];

                if (float.IsNaN(flux) || float.IsInfinity(flux))
                    continue;

                if (flux > maxFlux) maxFlux = flux;
            }

            Console.WriteLine($"  최대 유량: {maxFlux:F4}");

            if (maxFlux < 0.0001f)
            {
                Console.WriteLine("  ⚠️ 경고: 유량이 거의 0입니다!");
                maxFlux = 1.0f;
            }

            // Bitmap 생성
            using (Bitmap bmp = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bmp.LockBits(
                    new Rectangle(0, 0, _width, _height),
                    ImageLockMode.WriteOnly,
                    bmp.PixelFormat
                );

                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;

                    for (int y = 0; y < _height; y++)
                    {
                        for (int x = 0; x < _width; x++)
                        {
                            int srcIndex = ((_height - 1 - y) * _width + x) * 2;
                            int dstIndex = (y * _width + x) * 4;

                            float flux = waterData[srcIndex + 1];  // G 채널

                            if (float.IsNaN(flux) || float.IsInfinity(flux))
                                flux = 0f;

                            // 정규화
                            float normalized = flux / maxFlux;
                            normalized = Math.Max(0f, Math.Min(1f, normalized));
                            byte value = (byte)(normalized * 255);

                            // 그레이스케일
                            ptr[dstIndex + 0] = value;
                            ptr[dstIndex + 1] = value;
                            ptr[dstIndex + 2] = value;
                            ptr[dstIndex + 3] = 255;
                        }
                    }
                }

                bmp.UnlockBits(bmpData);
                bmp.Save(outputPath, ImageFormat.Png);
            }

            Console.WriteLine($"[WaterFlow] Flux Map 저장 완료: {outputPath}");
        }

        /// <summary>
        /// Valley Map을 PNG 파일로 저장
        /// </summary>
        /// <param name="valleyMap">저장할 Valley Map 텍스처</param>
        /// <param name="filePath">저장 경로 (예: "valley_map.png")</param>
        public void SaveValleyMapToPNG(Texture valleyMap, string filePath)
        {
            Console.WriteLine($"[WaterFlow] Valley Map 저장 중: {filePath}");

            // OpenGL에서 텍스처 데이터 읽기 (R32F 포맷)
            float[] valleyData = new float[_width * _height];
            Gl.BindTexture(TextureTarget.Texture2d, valleyMap.TextureID);
            Gl.GetTexImage(
                TextureTarget.Texture2d,
                0,
                OpenGL.PixelFormat.Red,
                PixelType.Float,
                valleyData
            );
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // Bitmap 생성 (ARGB 포맷)
            Bitmap bitmap = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData bmpData = bitmap.LockBits(
                new Rectangle(0, 0, _width, _height),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            // float 값을 byte로 변환하여 비트맵에 쓰기
            unsafe
            {
                byte* ptr = (byte*)bmpData.Scan0;

                for (int y = 0; y < _height; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        int idx = y * _width + x;
                        float value = valleyData[idx];

                        // 0.0~1.0 → 0~255 변환
                        byte grayValue = (byte)(value * 255.0f);

                        // BGRA 순서로 저장 (OpenGL은 Y축이 반전됨)
                        int bmpIdx = ((_height - 1 - y) * _width + x) * 4;
                        ptr[bmpIdx + 0] = grayValue; // B
                        ptr[bmpIdx + 1] = grayValue; // G
                        ptr[bmpIdx + 2] = grayValue; // R
                        ptr[bmpIdx + 3] = 255;       // A
                    }
                }
            }

            bitmap.UnlockBits(bmpData);

            // PNG로 저장
            bitmap.Save(filePath, ImageFormat.Png);
            bitmap.Dispose();

            Console.WriteLine($"[WaterFlow] Valley Map 저장 완료: {filePath}");
        }

        public void SaveRiverMap(Texture waterTexture, string filePath)
        {
            Console.WriteLine($"[River] 강줄기 맵 저장 중: {filePath}");

            // RG 채널 데이터 읽기 (2개 채널)
            float[] textureData = new float[_width * _height * 2];

            Gl.BindTexture(TextureTarget.Texture2d, waterTexture.TextureID);
            Gl.GetTexImage(TextureTarget.Texture2d, 0, OpenGL.PixelFormat.Rg, PixelType.Float, textureData);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // G 채널(Flux)의 최대값 찾기 (정규화용)
            float maxFlux = 0.0001f;
            for (int i = 1; i < textureData.Length; i += 2) // G채널은 index 1, 3, 5...
            {
                if (textureData[i] > maxFlux) maxFlux = textureData[i];
            }

            Console.WriteLine($"  최대 유량(Flux): {maxFlux}");

            // 비트맵 생성
            using (Bitmap bitmap = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                BitmapData bmpData = bitmap.LockBits(
                    new Rectangle(0, 0, _width, _height),
                    ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );

                unsafe
                {
                    byte* ptr = (byte*)bmpData.Scan0;

                    for (int y = 0; y < _height; y++)
                    {
                        for (int x = 0; x < _width; x++)
                        {
                            int pixelIdx = (y * _width + x) * 2; // 데이터는 RG 순서

                            float depth = textureData[pixelIdx];     // R: 수심 (호수)
                            float flux = textureData[pixelIdx + 1];  // G: 유량 (강)

                            // 시각화 로직:
                            // 1. 유량(Flux)을 로그 스케일로 변환 (작은 지류도 보이게)
                            // 2. 파란색 계열로 표현

                            float normalizedFlux = (float)Math.Log(flux * 1000.0 + 1.0) / (float)Math.Log(maxFlux * 1000.0 + 1.0);
                            normalizedFlux = Math.Min(1.0f, normalizedFlux);

                            byte b = (byte)(normalizedFlux * 255);
                            byte g = (byte)(normalizedFlux * 100); // 약간 청록색 느낌
                            byte r = 0;

                            // 만약 깊은 물(호수)라면 진한 파랑 추가
                            if (depth > 0.1f)
                            {
                                b = 255;
                                g = (byte)Math.Min(255, g + 50);
                            }

                            // 배경은 검은색, 강은 파란색
                            int bmpIdx = ((_height - 1 - y) * _width + x) * 4;
                            ptr[bmpIdx + 0] = b; // B
                            ptr[bmpIdx + 1] = g; // G
                            ptr[bmpIdx + 2] = r; // R
                            ptr[bmpIdx + 3] = 255; // A
                        }
                    }
                }
                bitmap.UnlockBits(bmpData);
                bitmap.Save(filePath, ImageFormat.Png);
            }
        }

        
        /// <summary>
        /// 리소스 정리
        /// </summary>
        public void Cleanup()
        {
            Gl.DeleteTextures(_waterBuffer0);
            Gl.DeleteTextures(_waterBuffer1);
            _shader.CleanUp();
        }
    }
}
using Common;
using Common.Abstractions;
using OpenGL;
using Shader;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using ZetaExt;

namespace Terrain
{
    public class WaterFlowGenerator
    {
        uint _mapTextureId = 0;
        uint _quadVAO;
        uint _quadVBO;

        int _width;
        int _height;
        uint _waterBuffer0;
        uint _waterBuffer1;
        uint _calcBuffer;
        uint _fluxBuffer;
        uint _riverMask;
        uint _riverMaskPass2Temp;   // Pass 2 임시 버퍼
        uint _riverMaskPass2Out;    // Pass 2 출력: 스무딩
        uint _riverMaskPass3Temp;   // Pass 3 임시 버퍼
        uint _riverMaskFinal;       // Pass 3 출력: 최종 결과

        bool _useBuffer0 = true;

        WaterFlowComputeShader _compShader;
        RiverMaskPass1ComputeShader _riverMaskPass1Shader;
        RiverMaskPass2ComputeShader _riverMaskPass2Shader;
        RiverMaskPass3ComputeShader _riverMaskPass3Shader;
        DisplayShader _displayShader;
                
        float _time = 0.0f;

        // ---------------------------------------------------------------------------
        // 속성
        // ---------------------------------------------------------------------------
        public uint WriteBuffer => _useBuffer0 ? _waterBuffer0 : _waterBuffer1;
        public uint ReadBuffer => _useBuffer0 ? _waterBuffer1 : _waterBuffer0;
        public uint RiverMaskFinal => _riverMaskFinal;

        // ---------------------------------------------------------------------------
        // 생성자
        // ---------------------------------------------------------------------------

        public WaterFlowGenerator()
        {
            _waterBuffer0 = 0;
            _waterBuffer1 = 0;
            _calcBuffer = 0;
            _fluxBuffer = 0;

            _compShader = new WaterFlowComputeShader(StrRes.PROJECT_PATH);
            _riverMaskPass1Shader = new RiverMaskPass1ComputeShader(StrRes.PROJECT_PATH);
            _riverMaskPass2Shader = new RiverMaskPass2ComputeShader(StrRes.PROJECT_PATH);
            _riverMaskPass3Shader = new RiverMaskPass3ComputeShader(StrRes.PROJECT_PATH);
            _displayShader = new DisplayShader(StrRes.PROJECT_PATH);

            CreateFullscreenQuad();
        }

        private void CreateFullscreenQuad()
        {
            float[] vertices = {
                -1.0f,  1.0f,   0.0f, 1.0f,
                -1.0f, -1.0f,   0.0f, 0.0f,
                 1.0f, -1.0f,   1.0f, 0.0f,

                -1.0f,  1.0f,   0.0f, 1.0f,
                 1.0f, -1.0f,   1.0f, 0.0f,
                 1.0f,  1.0f,   1.0f, 1.0f
            };

            _quadVAO = Gl.GenVertexArray();
            Gl.BindVertexArray(_quadVAO);

            _quadVBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ArrayBuffer, _quadVBO);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(sizeof(float) * vertices.Length),
                          vertices, BufferUsage.StaticDraw);

            Gl.VertexAttribPointer(0, 2, VertexAttribType.Float, false,
                                   4 * sizeof(float), IntPtr.Zero);
            Gl.EnableVertexAttribArray(0);

            Gl.VertexAttribPointer(1, 2, VertexAttribType.Float, false,
                                   4 * sizeof(float), new IntPtr(2 * sizeof(float)));
            Gl.EnableVertexAttribArray(1);

            Gl.BindVertexArray(0);
        }

        public string Load(string fileName)
        {
            // 원본 텍스처 로드
            Texture texture = new Texture(fileName);
            _width = texture.Width;
            _height = texture.Height;

            // 텍스처 데이터를 float 배열로 읽기
            Gl.BindTexture(TextureTarget.Texture2d, texture.TextureID);

            // RGBA 또는 RGB로 읽어온 후 R32F로 변환
            byte[] rgbaData = new byte[_width * _height * 4];
            Gl.GetTexImage(TextureTarget.Texture2d, 0,
                           OpenGL.PixelFormat.Rgba,
                           PixelType.UnsignedByte,
                           rgbaData);

            // float 배열로 변환 (R 채널만 사용, 정규화)
            float[] heightData = new float[_width * _height];
            for (int i = 0; i < _width * _height; i++)
            {
                // R 채널만 추출하고 0~1 범위로 정규화
                heightData[i] = rgbaData[i * 4];// / 255.0f;
            }

            // R32F 포맷으로 새 텍스처 생성
            _mapTextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _mapTextureId);

            Gl.TexImage2D(
                TextureTarget.Texture2d, 0, InternalFormat.R32f,
                _width, _height, 0,
                OpenGL.PixelFormat.Red,
                PixelType.Float,
                heightData
            );

            // 필터링 설정
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.LINEAR);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);

            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 원본 텍스처 해제
            Gl.DeleteTextures(texture.TextureID);

            string result = "";
            if (_waterBuffer0 == 0)
            {
                _waterBuffer0 = CreateWaterBuffer(_width, _height, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);
                _waterBuffer1 = CreateWaterBuffer(_width, _height, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);
                _calcBuffer = CreateWaterBuffer(_width, _height, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);
                _fluxBuffer = CreateWaterBuffer(_width, _height, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);
                _riverMask = CreateWaterBuffer(_width, _height, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);
                _riverMaskPass2Temp = CreateWaterBuffer(_width, _height, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);
                _riverMaskPass2Out = CreateWaterBuffer(_width, _height, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);
                _riverMaskPass3Temp = CreateWaterBuffer(_width, _height, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);
                _riverMaskFinal = CreateWaterBuffer(_width, _height, InternalFormat.Rgba32f, OpenGL.PixelFormat.Rgba);

                result +=($"[WaterFlow] 버퍼 생성 완료 - Buffer0: {_waterBuffer0}, Buffer1: {_waterBuffer1}\r\n");
            }

            result += ($"맵 텍스처 id{_mapTextureId} {_width}x{_height} (R32F로 변환) 로드 완료: " + Path.GetFileName(fileName) + "\r\n");

            return result;
        }

        public void Move(float flowVelocityConstant = 0.1f, float evaporationFactor = 0.001f)
        {
            _compShader.Bind();
            {
                _compShader.SetMode(3);
                _compShader.BindBuffers(_mapTextureId, ReadBuffer, WriteBuffer, _calcBuffer, _fluxBuffer);
                _compShader.Dispatch(_width, _height);
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);

                _compShader.SetMode(4);
                _compShader.SetFlowVelocityConstant(flowVelocityConstant);
                _compShader.SetEvaporationBaseRate(evaporationFactor);
                _compShader.BindBuffers(_mapTextureId, ReadBuffer, WriteBuffer, _calcBuffer, _fluxBuffer);
                _compShader.Dispatch(_width, _height);
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
            }
            _compShader.Unbind();

            // 버퍼 스왑 추가!
            _useBuffer0 = !_useBuffer0;
        }

        public void RunRiverMaskPass1(float minWaterDepth = 0.05f, float minFluxMagnitude = 0.01f, float deepWaterDepth = 0.2f)
        {
            _riverMaskPass1Shader.Bind();
            {
                _riverMaskPass1Shader.SetParameters(minWaterDepth, minFluxMagnitude, deepWaterDepth);
                _riverMaskPass1Shader.BindBuffers(WriteBuffer, _riverMask);
                _riverMaskPass1Shader.Dispatch(_width, _height);
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            }
            _riverMaskPass1Shader.Unbind();
        }

        public void RunRiverMaskPass2(int iterations)
        {
            // Step 2: Pass 2 실행 (Opening)
            _riverMaskPass2Shader.PerformOpening(
                _riverMask,      // 입력
                _riverMaskPass2Temp,     // 임시 버퍼
                _riverMaskPass2Out,    // 출력
                _width, _height,
                iterations: iterations      // Erosion 1회
            );
        }

        public void RunRiverMaskPass3()
        {
            // Pass 3: Multi-pass Blur (매우 부드러움)
            _riverMaskPass3Shader.PerformMultiPassBlur(
                _riverMaskPass2Out,
                _riverMaskPass3Temp,
                _riverMask,        // 재활용
                _riverMaskFinal,
                _width,
                _height,
                passes: 10,              // 블러 2회
                sigma: 1.5f             // 강한 블러
            );
        }

        public void Clear()
        {
            ClearWaterBuffer(_waterBuffer0);
            ClearWaterBuffer(_waterBuffer1);
            ClearWaterBuffer(_calcBuffer);
            _useBuffer0 = true;
        }

        public void RunAddWater(float amount = 0.01f)
        {
            _compShader.Bind();
            {
                _compShader.SetMode(0);
                _compShader.SetRainWaterAmount(amount);
                _compShader.LoadUniforms(new Vertex2f(1000.0f, 1000.0f), 1000.0f);
                Dispatch();
            }
            _compShader.Unbind();
        }

        public void RunRandomAddWater()
        {
            _compShader.Bind();
            {
                float rx = Rand.NextFloat * 2000.0f;
                float ry = Rand.NextFloat * 2000.0f;

                _compShader.LoadUniforms(new Vertex2f(rx, ry), 200.0f);
                _compShader.SetMode(1);
                Dispatch();
            }
            _compShader.Unbind();
        }

        public void Dispatch()
        {
            _compShader.BindBuffers(_mapTextureId, ReadBuffer, WriteBuffer, _calcBuffer, _fluxBuffer);
            _compShader.Dispatch(_width, _height);
            Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit | MemoryBarrierMask.TextureFetchBarrierBit);
            _useBuffer0 = !_useBuffer0;
        }

        public void WriteGradient()
        {
            _compShader.Bind();
            {
                _compShader.SetMode(2);
                Dispatch();
            }
            _compShader.Unbind();
        }

        public void Render(float deltaTime, float scaled, bool useHeightMap)
        {
            // 시간 누적
            _time += deltaTime;

            // 현재 사용 중인 버퍼 선택
            uint currentBuffer = WriteBuffer;

            // Depth test 비활성화 (fullscreen quad이므로)
            Gl.Disable(EnableCap.DepthTest);

            // 쉐이더 바인딩 및 Uniform 설정
            _displayShader.Bind();
            _displayShader.LoadNoiseTexture(TextureUnit.Texture0, currentBuffer);
            _displayShader.LoadHeightMapTexture(TextureUnit.Texture1, _mapTextureId);
            _displayShader.LoadScaled(scaled);
            _displayShader.LoadUseHeightMap(useHeightMap);
            _displayShader.LoadFlip(false);

            Gl.BindVertexArray(_quadVAO);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.BindVertexArray(0);
            _displayShader.Unbind();

            // 상태 복원
            Gl.Enable(EnableCap.DepthTest);
        }

        public void RenderRiver(float deltaTime, float scaled, bool useHeightMap)
        {
            // 시간 누적
            _time += deltaTime;

            // 현재 사용 중인 버퍼 선택
            uint currentBuffer = WriteBuffer;

            // Depth test 비활성화 (fullscreen quad이므로)
            Gl.Disable(EnableCap.DepthTest);

            // 쉐이더 바인딩 및 Uniform 설정
            _displayShader.Bind();
            _displayShader.LoadNoiseTexture(TextureUnit.Texture0, _riverMaskFinal);
            _displayShader.LoadHeightMapTexture(TextureUnit.Texture1, _mapTextureId);
            _displayShader.LoadScaled(scaled);
            _displayShader.LoadUseHeightMap(useHeightMap);
            _displayShader.LoadFlip(false);

            Gl.BindVertexArray(_quadVAO);
            Gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
            Gl.BindVertexArray(0);
            _displayShader.Unbind();

            // 상태 복원
            Gl.Enable(EnableCap.DepthTest);
        }

        private uint CreateWaterBuffer(int width, int height, InternalFormat internalFormat, OpenGL.PixelFormat pixelFormat)
        {
            uint buffer = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, buffer);

            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                internalFormat,      // ✅ Rgba32f 변경 (4채널), InternalFormat.Rgba32f
                width, height, 0,
                pixelFormat,     // ✅ 일치 OpenGL.PixelFormat.Rgba
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

            Console.WriteLine($"[WaterFlow {buffer}] 버퍼 생성됨");
            return buffer;
        }

        private void ClearWaterBuffer(uint buffer, float value = 0.0f)
        {
            if (buffer == 0) return;

            float[] data = new float[_width * _height * 4];  // RG = 2채널

            // depth(R)와 flux(G) 모두 0.5로 초기화
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = value;
            }

            Gl.BindTexture(TextureTarget.Texture2d, buffer);
            Gl.TexSubImage2D(
                TextureTarget.Texture2d,
                0, 0, 0,
                _width,
                _height,
                OpenGL.PixelFormat.Rgba,     // ✅ Rgba 포맷
                PixelType.Float,
                data
            );
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            Console.WriteLine($"[WaterFlow {buffer}] 버퍼 초기화됨, 값=0.5");
        }

        public Bitmap ExportWaterMapToPNG(uint textureId, bool isThresold = false)
        {
            float[] waterData = new float[_width * _height * 4];
            Gl.BindTexture(TextureTarget.Texture2d, textureId);
            Gl.GetTexImage(
                TextureTarget.Texture2d,
                0,
                OpenGL.PixelFormat.Rgba,
                PixelType.Float,
                waterData
            );
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 최대값 찾기
            float maxR = 0f, maxG = 0f, maxB = 0f;
            for (int i = 0; i < waterData.Length; i += 4)
            {
                float r = waterData[i];
                float g = waterData[i + 1];
                float b = waterData[i + 2];

                if (!float.IsNaN(r) && !float.IsInfinity(r))
                    maxR = Math.Max(maxR, Math.Abs(r));
                if (!float.IsNaN(g) && !float.IsInfinity(g))
                    maxG = Math.Max(maxG, Math.Abs(g));
                if (!float.IsNaN(b) && !float.IsInfinity(b))
                    maxB = Math.Max(maxB, Math.Abs(b));
            }

            // 0으로 나누기 방지
            if (maxR < 0.0001f) maxR = 255.0f;
            if (maxG < 0.0001f) maxG = 255.0f;
            if (maxB < 0.0001f) maxB = 255.0f;

            Bitmap bmp = new Bitmap(_width, _height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
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
                        int srcIndex = ((_height - 1 - y) * _width + x) * 4;
                        int dstIndex = (y * _width + x) * 4;

                        float r = waterData[srcIndex];
                        float g = waterData[srcIndex + 1];
                        float b = waterData[srcIndex + 2];

                        if (float.IsNaN(r) || float.IsInfinity(r)) r = 0f;
                        if (float.IsNaN(g) || float.IsInfinity(g)) g = 0f;
                        if (float.IsNaN(b) || float.IsInfinity(b)) b = 0f;

                        // 정규화 (0~255)
                        byte rByte = (byte)MathF.Clamp(r / maxR * 255f, 0, 255);
                        byte gByte = (byte)MathF.Clamp(g / maxG * 255f, 0, 255);
                        byte bByte = (byte)MathF.Clamp(b / maxB * 255f, 0, 255);


                        if (isThresold)
                        {
                            rByte = (rByte > 10) ? (byte)255 : (byte)0;
                            gByte = (gByte > 10) ? (byte)255 : (byte)0;
                            bByte = (bByte > 10) ? (byte)255 : (byte)0;
                        }

                        ptr[dstIndex + 0] = bByte;  // B
                        ptr[dstIndex + 1] = gByte;  // G
                        ptr[dstIndex + 2] = rByte;  // R
                        ptr[dstIndex + 3] = 255;    // A
                    }
                }
            }
            bmp.UnlockBits(bmpData);
            return bmp;
        }
    }
}
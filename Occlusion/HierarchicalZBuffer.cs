using Common.Abstractions;
using Geometry;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZetaExt;

namespace Occlusion
{
    /// <summary>
    /// 계층적 Z-버퍼를 구현한 클래스입니다.
    /// 깊이 맵의 다양한 해상도 레벨을 관리하여 occlusion culling을 수행합니다.
    /// 참고문헌: Hierarchical-Z map based occlusion culling
    /// </summary>
    public class HierarchicalZBuffer
    {
        const int POSITION_ATTRIB = 0;
        const int TEXCOORD_ATTRIB = 1;
        const int PATCH_VERTICES = 4;

        readonly uint _fbo;                         // FBO
        readonly uint[] _hzbTextures;               // 계층별 텍스처
        readonly uint _depthTexture;                // 깊이 텍스처
        readonly uint _colorTexture;                // 색상 텍스처
        readonly int _levels;                       // 계층 수
        readonly int _width;                        // Z-Buffer 초기 너비
        readonly int _height;                       // Z-Buffer 초기 높이
        HzmMipmapShader _mipmapShader;              // 밉맵쉐이더
        List<float[]> _zbuffer;
        TerrainDepthShader _terrainDepthShader;     // 간단지형 쉐이더
        HzbComputeShader _computeShader;            // 컴퓨트 셰이더

        public uint Framebuffer => _fbo;
        public uint DepthTexture => _depthTexture;
        public int Levels => _levels;
        public int Width => _width;
        public int Height => _height;

        /// <summary>
        /// 계층적 Z-버퍼(Hierarchical Z-Buffer)를 초기화하는 클래스입니다.
        /// </summary>
        /// <remarks>
        /// 성능 최적화를 위해 120x67 해상도를 권장합니다.
        /// 여러 레벨의 깊이 텍스처를 생성하여 계층적 Z-버퍼 구조를 구축합니다.
        /// </remarks>
        /// <param name="width">Z-버퍼의 너비 (픽셀 단위)</param>
        /// <param name="height">Z-버퍼의 높이 (픽셀 단위)</param>
        /// <param name="projectPath">셰이더 파일이 위치한 프로젝트의 루트 경로</param>
        public HierarchicalZBuffer(int width, int height, string projectPath)
        {
            // 기본 속성 초기화
            _width = width;
            _height = height;
            _levels = (int)Math.Floor(Math.Log(Math.Max(width, height), 2)) + 1;
            _hzbTextures = new uint[_levels];

            // 셰이더 초기화
            if (_mipmapShader == null) _mipmapShader = new HzmMipmapShader(projectPath);
            if (_terrainDepthShader == null) _terrainDepthShader = new TerrainDepthShader(projectPath);
            if (_computeShader == null) _computeShader = new HzbComputeShader(projectPath);

            // 프레임버퍼 생성
            _fbo = Gl.GenFramebuffer();

            // 컬러 텍스처 초기화 (R16F 포맷)
            _colorTexture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _colorTexture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.R16f, _width, _height, 0, PixelFormat.Red, PixelType.UnsignedByte, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 깊이 텍스처 초기화 (32비트 부동소수점 포맷)
            _depthTexture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _depthTexture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent32f, _width, _height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            float[] borderColor = { 1.0f, 1.0f, 1.0f, 1.0f };
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureBorderColor, borderColor);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 각 레벨별 HZB 텍스처 생성 (32비트 부동소수점 포맷)
            for (int i = 0; i < _levels; i++)
            {
                _hzbTextures[i] = Gl.GenTexture();
                Gl.BindTexture(TextureTarget.Texture2d, _hzbTextures[i]);
                Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.R32f, _width >> i, _height >> i, 0, PixelFormat.Red, PixelType.Float, IntPtr.Zero);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            }
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 프레임버퍼에 텍스처 연결
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, _colorTexture, 0);
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, _depthTexture, 0);

            // CPU 측 Z-버퍼 배열 초기화
            if (_zbuffer == null)
            {
                _zbuffer = new List<float[]>();
                for (int i = 0; i < _levels; i++)
                {
                    int w = _width >> i;
                    int h = _height >> i;
                    float[] depthValues = new float[w * h];
                    _zbuffer.Add(depthValues);
                }
            }
        }

        /// <summary>
        /// GPU에서 CPU로 모든 레벨의 깊이 맵 데이터를 읽어옵니다.
        /// </summary>
        /// <param name="maxDepth">생성할 최대 레벨 깊이. 0부터 시작하여 maxDepth-1 레벨까지 생성됩니다.</param>
        private void ReadZBuffersFromGPU(int maxDepth = -1)
        {
            // 최대 레벨까지 생성한다.
            if (maxDepth == -1) maxDepth = _levels;

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

            // 계층별 텍스처 생성
            for (int i = 0; i < maxDepth; i++)
            {
                // 현재 프레임버퍼에서 깊이 데이터 읽기
                int w = _width >> i;
                int h = _height >> i;
                float[] depthValues = _zbuffer[i];

                // 또는 텍스처에서 직접 읽기
                Gl.GetTextureImage(_hzbTextures[i], 0, PixelFormat.Red, PixelType.Float, depthValues.Length * sizeof(float), depthValues);
            }
        }

        /// <summary>
        /// GPU에서 계층적 Z 버퍼를 생성하고 CPU로 전송한 후,<br/>
        /// CPU에서 나머지 밉맵 레벨을 생성합니다.<br/>
        /// <br/>
        /// 처리 순서:<br/>
        /// 1. GPU에서 레벨 0의 Z 버퍼 생성<br/>
        /// 2. 레벨 0 버퍼를 GPU에서 CPU로 전송<br/>
        /// 3. CPU에서 레벨 1부터 최고 레벨까지의 밉맵 생성<br/>
        /// </summary>
        /// <remarks>
        /// 성능 이점:<br/>
        /// - GPU 작업 최소화: 레벨 0만 GPU에서 생성<br/>
        /// - 데이터 전송 최소화: 레벨 0만 GPU→CPU 전송<br/>
        /// - 멀티스레딩 CPU 활용: 병렬 처리로 효율적인 밉맵 생성<br/>
        /// - 오버헤드 감소: 컴퓨트 셰이더 디스패치와 메모리 배리어 없음
        /// </remarks>
        public void GenerateZBuffer()
        {
            GenerateHierachyZBufferOnGPU(maxDepth: 0);
            ReadZBuffersFromGPU(maxDepth: 0);
            GenerateMipMapBufferOnCPU(fromLevel: 1, toLevel: -1);
        }

        /// <summary>
        /// 컴퓨트 셰이더를 사용하여 계층적 Z-버퍼의 모든 밉맵 레벨을 GPU에서 생성합니다.<br/>
        /// 큰 해상도(512x512 이상)에서 더 효율적인 방식입니다.<br/>
        /// </summary>
        /// <param name="maxLevel">생성할 최대 레벨 (-1은 모든 레벨)</param>
        /// <remarks>
        /// 성능 특성:<br/>
        /// - 작은 해상도에서는 GenerateZBuffer()보다 느릴 수 있음<br/>
        /// - GPU-GPU 메모리 전송 오버헤드와 메모리 배리어 비용 발생<br/>
        /// - 모든 레벨에 대한 별도 디스패치 오버헤드 발생<br/>
        /// - 1024x1024 이상 해상도에서 확실한 성능 이점 발휘<br/>
        /// </remarks>
        public void GenerateMipmapsUsingCompute(int maxLevel = -1)
        {
            if (_computeShader == null)
                throw new InvalidOperationException("컴퓨트 셰이더가 초기화되지 않았습니다.");

            // 최대 레벨 설정
            if (maxLevel < 0)
                maxLevel = _levels - 1;

            // 기본 레벨 0의 Z-버퍼 생성 (기존 방식)
            GenerateHierachyZBufferOnGPU(maxDepth: 0);

            // 컴퓨트 셰이더 바인딩
            _computeShader.Bind();

            // 각 밉맵 레벨에 대해 컴퓨트 셰이더 실행 (레벨 1부터 시작)
            for (int level = 1; level <= maxLevel; level++)
            {
                // 입력 크기 계산 (이전 레벨)
                int inputWidth = _width >> (level - 1);
                int inputHeight = _height >> (level - 1);

                // 출력 크기 계산 (현재 레벨)
                int outputWidth = _width >> level;
                int outputHeight = _height >> level;

                // 셰이더 유니폼 설정
                _computeShader.LoadUniform(HzbComputeShader.UNIFORM_NAME.inputSize, new Vertex2i(inputWidth, inputHeight));
                _computeShader.LoadUniform(HzbComputeShader.UNIFORM_NAME.mipLevel, level);

                // 이미지 바인딩 (입력: 이전 레벨, 출력: 현재 레벨)
                Gl.BindImageTexture(0, _hzbTextures[level - 1], 0, false, 0, BufferAccess.ReadOnly, InternalFormat.R32f);
                Gl.BindImageTexture(1, _hzbTextures[level], 0, false, 0, BufferAccess.WriteOnly, InternalFormat.R32f);

                // 메모리 배리어 설정: 이전 레벨의 이미지가 모두 기록된 후 읽기 시작하도록 보장
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);

                // 컴퓨트 셰이더 디스패치
                // 16x16 작업 그룹 크기에 맞춰서 계산
                int groupsX = (int)Math.Ceiling(outputWidth / 16.0);
                int groupsY = (int)Math.Ceiling(outputHeight / 16.0);
                Gl.DispatchCompute((uint)groupsX, (uint)groupsY, 1);

                // 메모리 배리어 설정: 이 레벨의 이미지 기록이 완료된 후 다음 레벨 처리 시작
                Gl.MemoryBarrier(MemoryBarrierMask.ShaderImageAccessBarrierBit);
            }

            // 셰이더 언바인딩
            _computeShader.Unbind();

            // 필요한 경우 CPU로 모든 레벨의 데이터 읽기
            if (_zbuffer != null)
            {
                ReadZBuffersFromGPU();
            }
        }

        /// <summary>
        /// CPU에서 깊이 밉맵을 멀티스레드로 생성합니다.
        /// 상위 레벨의 4개(2x2) 픽셀을 비교하여 최대 깊이값을 하위 레벨에 저장합니다.
        /// 너비나 높이가 홀수인 경우 추가 픽셀도 고려합니다.
        /// </summary>
        /// <param name="fromLevel">생성을 시작할 레벨 인덱스</param>
        /// <param name="toLevel">생성을 종료할 레벨 인덱스 (-1: 최대 레벨까지)</param>
        /// <param name="maxThreads">사용할 최대 스레드 수 (기본값: 환경 프로세서 수)</param>
        private void GenerateMipMapBufferOnCPU(int fromLevel = 0, int toLevel = -1, int maxThreads = -1)
        {
            // 최대 레벨까지 생성
            if (toLevel == -1) toLevel = _levels - 1;

            // 사용 가능한 스레드 수 결정 (기본값은 환경 프로세서 수)
            if (maxThreads <= 0)
                maxThreads = Environment.ProcessorCount;

            // 각 레벨별로 밉맵 처리
            for (int i = fromLevel; i <= toLevel; i++)
            {
                // 상위 레벨(부모)의 크기 계산
                int parentWidth = _width >> (i - 1);
                int parentHeight = _height >> (i - 1);

                // 현재 레벨의 크기 계산
                int currWidth = _width >> i;
                int currHeight = _height >> i;

                // 상위/현재 레벨 버퍼
                float[] parentBuffer = _zbuffer[i - 1];
                float[] currentBuffer = _zbuffer[i];

                // 멀티스레딩을 위한 작업 분할
                int linesPerThread = (int)Math.Ceiling((double)currHeight / maxThreads);

                // 작업(Task) 배열 생성
                Task[] tasks = new Task[maxThreads];

                for (int threadId = 0; threadId < maxThreads; threadId++)
                {
                    // 로컬 변수 복사 (캡처를 위해)
                    int localThreadId = threadId;
                    int localParentWidth = parentWidth;
                    int localParentHeight = parentHeight;
                    int localCurrWidth = currWidth;
                    int localCurrHeight = currHeight;

                    // 현재 스레드가 처리할 행의 범위 계산
                    int startY = localThreadId * linesPerThread;
                    int endY = Math.Min((localThreadId + 1) * linesPerThread, currHeight);

                    // 작업 생성 및 시작
                    tasks[threadId] = Task.Run(() =>
                    {
                        // 각 스레드는 할당된 행의 범위만 처리
                        for (int y = startY; y < endY; y++)
                        {
                            for (int x = 0; x < localCurrWidth; x++)
                            {
                                // 2x2 픽셀 영역의 시작점
                                int m = 2 * x;
                                int n = 2 * y;

                                // 2x2 픽셀의 깊이값 읽기
                                float topLeft = parentBuffer[(n + 0) * localParentWidth + (m + 0)];
                                float topRight = parentBuffer[(n + 0) * localParentWidth + (m + 1)];
                                float bottomLeft = parentBuffer[(n + 1) * localParentWidth + (m + 0)];
                                float bottomRight = parentBuffer[(n + 1) * localParentWidth + (m + 1)];

                                // 최대 깊이값 계산
                                float maxDepth = Math.Max(Math.Max(topLeft, bottomLeft),
                                                       Math.Max(topRight, bottomRight));

                                // 홀수 너비 처리인 경우에 오른쪽 가장자리는 3개 픽셀을 비교한다.
                                if ((localParentWidth & 1) == 1 && m + 2 < localParentWidth)
                                {
                                    if (x == localCurrWidth - 1)  // 마지막 열인 경우만
                                    {
                                        float extraTopRight = parentBuffer[(n + 0) * localParentWidth + (m + 2)];
                                        float extraBottomRight = parentBuffer[(n + 1) * localParentWidth + (m + 2)];
                                        maxDepth = Math.Max(maxDepth, Math.Max(extraTopRight, extraBottomRight));
                                    }
                                }

                                // 홀수 높이 처리인 경우에 아랫쪽 가장자리는 3개 픽셀을 비교한다.
                                if ((localParentHeight & 1) == 1 && n + 2 < localParentHeight)
                                {
                                    if (y == localCurrHeight - 1)  // 마지막 행인 경우만
                                    {
                                        float extraBottomLeft = parentBuffer[(n + 2) * localParentWidth + (m + 0)];
                                        float extraBottomRight = parentBuffer[(n + 2) * localParentWidth + (m + 1)];
                                        maxDepth = Math.Max(maxDepth, Math.Max(extraBottomLeft, extraBottomRight));
                                    }
                                }

                                // 결과 저장
                                currentBuffer[y * localCurrWidth + x] = maxDepth;
                            }
                        }
                    });
                }

                // 모든 작업이 완료될 때까지 대기
                Task.WaitAll(tasks);
            }
        }

        /// <summary>
        /// 폐색체들을 간단하게 렌더링합니다.
        /// </summary>
        /// <param name="shader">사용할 간단한 셰이더</param>
        /// <param name="entities">렌더링할 폐색체 목록</param>
        /// <param name="view">뷰 행렬</param>
        /// <param name="proj">투영 행렬</param>
        /// <exception cref="ArgumentNullException">entity나 model이 null인 경우</exception>
        public void RenderSimpleEntity(SimpleDepthShader shader, List<PhysicalRenderEntity> entities, Matrix4x4f view, Matrix4x4f proj)
        {
            // 블렌딩 비활성화
            Gl.Disable(EnableCap.Blend);

            shader.Bind();
            shader.LoadUniform(SimpleDepthShader.UNIFORM_NAME.proj, proj);
            shader.LoadUniform(SimpleDepthShader.UNIFORM_NAME.view, view);

            foreach (PhysicalRenderEntity entity in entities)
            {
                // 유효성 검사
                if (entity?.Model == null || entity.Model.Length == 0)
                    throw new ArgumentNullException(nameof(entity));

                shader.LoadUniform(SimpleDepthShader.UNIFORM_NAME.model, entity.ModelMatrix);

                // 모델을 그린다
                foreach (RawModel3d rawModel in entity.Model)
                {
                    Gl.BindVertexArray(rawModel.VAO);
                    Gl.EnableVertexAttribArray(0);  // position

                    // 깊이맵 생성을 위한 렌더링
                    Gl.DrawArrays(PrimitiveType.Triangles, 0, rawModel.VertexCount);

                    // 상태 복원
                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }
            }

            shader.Unbind();
        }

        /// <summary>
        /// 지형을 간단하게 그려 지평선 깊이맵을 생성합니다.
        /// <code>
        /// 주의: 지형 렌더링시 오클루더로 나무와 같은 작은 오브젝트 사용은 권장하지 않습니다.
        /// 깜빡임 현상이 발생할 수 있습니다.
        /// </code>
        /// </summary>
        /// <param name="terrianPatchEntity">지형 패치</param> 
        /// <param name="proj">투영 행렬</param>
        /// <param name="view">뷰 행렬</param>
        /// <param name="heightScale"></param>
        public void RenderSimpleTerrain(Entity terrianPatchEntity, Matrix4x4f proj, Matrix4x4f view, float heightScale)
        {
            // 유효성 검사
            if (terrianPatchEntity == null) return;

            // 유효성 검사
            if (terrianPatchEntity?.Model == null || terrianPatchEntity.Model.Length == 0)
                throw new ArgumentNullException(nameof(terrianPatchEntity));

            TerrainDepthShader shader = _terrainDepthShader;
            Entity terrainEntity = terrianPatchEntity;
            TexturedModel terrainModel = terrainEntity.Model[0] as TexturedModel;

            // 쉐이더 설정
            shader.Bind();
            shader.LoadUniform(TerrainDepthShader.UNIFORM_NAME.proj, proj);
            shader.LoadUniform(TerrainDepthShader.UNIFORM_NAME.view, view);
            shader.LoadUniform(TerrainDepthShader.UNIFORM_NAME.model, terrainEntity.ModelMatrix);
            shader.LoadUniform(TerrainDepthShader.UNIFORM_NAME.heightScale, heightScale);
            shader.LoadTexture(TerrainDepthShader.UNIFORM_NAME.gHeightMap, TextureUnit.Texture0, 
                terrainModel.Texture == null? 0: terrainModel.Texture.TextureID);

            // 렌더링
            try
            {
                Gl.BindVertexArray(terrainModel.VAO);
                Gl.EnableVertexAttribArray(POSITION_ATTRIB);
                Gl.EnableVertexAttribArray(TEXCOORD_ATTRIB);
                Gl.BindBuffer(BufferTarget.ElementArrayBuffer, terrainModel.IBO);
                Gl.PatchParameter(PatchParameterName.PatchVertices, PATCH_VERTICES);
                Gl.DrawElements(PrimitiveType.Patches, terrainModel.VertexCount, DrawElementsType.UnsignedInt, IntPtr.Zero);
            }
            finally
            {
                // 상태 복원
                Gl.DisableVertexAttribArray(TEXCOORD_ATTRIB);
                Gl.DisableVertexAttribArray(POSITION_ATTRIB);
                Gl.BindVertexArray(0);
                shader.Unbind();
            }
        }

        /// <summary>
        /// 밉맵쉐이더를 이용하여 GPU에서 계층적 깊이 맵을 생성합니다.
        /// </summary>
        /// <param name="maxDepth">생성할 최대 레벨 깊이. 0부터 시작하여 maxDepth-1 레벨까지 생성됩니다.</param>
        /// <remarks>
        /// 처리 과정:
        /// 1. 밉맵 쉐이더 바인딩
        /// 2. 각 레벨별로 다음 작업 수행:
        ///    - 프레임버퍼에 현재 레벨의 텍스처 연결
        ///    - 뷰포트 크기 설정 (레벨에 따라 1/2씩 감소)
        ///    - 이전 레벨의 깊이 텍스처 로드 (0레벨은 기본 깊이 텍스처 사용)
        ///    - 현재 레벨의 크기 정보를 쉐이더에 전달
        ///    - 깊이 테스트 비활성화 후 렌더링
        /// 3. 프레임버퍼를 원래 상태로 복원
        /// </remarks>
        private void GenerateHierachyZBufferOnGPU(int maxDepth = -1)
        {
            // 최대 레벨까지 생성한다.
            if (maxDepth == -1) maxDepth = _levels - 1;

            _mipmapShader.Bind();
            for (int i = 0; i <= maxDepth; i++)
            {
                Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, _hzbTextures[i], 0);
                Gl.Viewport(0, 0, _width >> i, _height >> i);
                _mipmapShader.LoadTexture(HzmMipmapShader.UNIFORM_NAME.DepthBuffer, TextureUnit.Texture0, i == 0 ? _depthTexture : _hzbTextures[i - 1]);
                _mipmapShader.LoadUniform(HzmMipmapShader.UNIFORM_NAME.LastMipSize, new Vertex2i(_width >> i, _height >> i));
                Gl.Disable(EnableCap.DepthTest);
                Gl.DrawArrays(PrimitiveType.Points, 0, 1);
                Gl.Enable(EnableCap.DepthTest);
            }
            _mipmapShader.Unbind();

            // 프레임버퍼 상태 복원
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, _colorTexture, 0);
        }

        /// <summary>
        /// AABB가 시야에 보이는지 검사합니다.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="aabb">검사할 AABB</param>
        /// <returns>AABB가 보이면 true, 가려졌으면 false</returns>
        public bool IsVisible(Matrix4x4f vp, Matrix4x4f view, AABB aabb)
        {
            AABB trans = aabb.TransformViewSpace(vp, view);
            return !IsOccluded(trans.LowerBound.x, trans.LowerBound.y, trans.LowerBound.z, trans.UpperBound.x, trans.UpperBound.y, trans.UpperBound.z);
        }

        /// <summary>
        /// 지정된 레벨에서의 너비와 높이를 계산하여 반환합니다.
        /// 각 레벨마다 이전 레벨의 1/2 크기로 축소됩니다.
        /// </summary>
        /// <param name="level">계산할 레벨 (0: 원본 크기)</param>
        /// <returns>해당 레벨에서의 너비와 높이</returns>
        public Vertex2i GetLevelResolution(int level)
        {
            int cw = _width >> (int)level;
            int ch = _height >> (int)level;
            return new Vertex2i(cw, ch);
        }

        public void DrawDepthBuffer(HzmDepthShader shader, Camera camera, int level)
        {
            shader.Bind();
            shader.LoadUniform(HzmDepthShader.UNIFORM_NAME.IsPerspective, false);
            shader.LoadUniform(HzmDepthShader.UNIFORM_NAME.LOD, level);
            shader.LoadUniform(HzmDepthShader.UNIFORM_NAME.CameraNear, camera.NEAR);
            shader.LoadUniform(HzmDepthShader.UNIFORM_NAME.CameraFar, camera.FAR);
            shader.LoadTexture(HzmDepthShader.UNIFORM_NAME.DepthTexture, TextureUnit.Texture0, _hzbTextures[level]);
            Gl.Disable(EnableCap.DepthTest);
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);
            Gl.Enable(EnableCap.DepthTest);
            shader.Unbind();
        }

        /// <summary>
        /// 스크린 공간 크기를 기반으로 적절한 LOD 레벨을 계산합니다.
        /// LOD 레벨부터 시작해서 레벨 0까지 계층적으로 깊이값을 검사합니다
        /// 높은 LOD 레벨(coarse)에서 낮은 레벨(fine)로 검사함으로써 성능을 최적화합니다
        /// </summary>
        /// <param name="width">스크린 공간(0-1)에서의 가로 크기</param>
        /// <param name="height">스크린 공간(0-1)에서의 세로 크기</param>
        /// <returns>계산된 LOD 레벨</returns>
        private int CalculateLODLevel(int width, int height)
        {
            return (int)Math.Ceiling(MathF.Log2(Math.Max(width, height) * 0.5f));
        }

        /// <summary>
        /// AABB가 깊이 버퍼에 의해 가려졌는지 검사한다.
        /// </summary>
        /// <param name="minx">AABB의 최소 x 좌표 (NDC)</param>
        /// <param name="miny">AABB의 최소 y 좌표 (NDC)</param>
        /// <param name="minz">AABB의 최소 z 깊이값</param>
        /// <param name="maxx">AABB의 최대 x 좌표 (NDC)</param>
        /// <param name="maxy">AABB의 최대 y 좌표 (NDC)</param>
        /// <param name="maxz">AABB의 최대 z 깊이값</param>
        /// <param name="lod">검사할 LOD 레벨</param>
        /// <returns>AABB가 완전히 가려졌으면 true, 아니면 false를 반환한다</returns>
        private bool IsOccluded(float minx, float miny, float minz, float maxx, float maxy, float maxz)
        {
            const float NDC_TRANSFORM = 0.5f;   // NDC변환을 위한 상수
            const int EXTRA_PADDING = 1;        // 패딩 크기

            // 카메라 좌표공간에서 물체가 앞, 뒤로 걸쳐있는 경우는 무조건 가려지지 않는 것으로 하드코딩한다.
            if (minz < 0 && maxz > 0) return false;

            // NDC 좌표계를 스크린 좌표계로 변환 ([-1,1] -> [0,1])
            minx = NDC_TRANSFORM * minx + NDC_TRANSFORM;
            miny = NDC_TRANSFORM * miny + NDC_TRANSFORM;
            maxx = NDC_TRANSFORM * maxx + NDC_TRANSFORM;
            maxy = NDC_TRANSFORM * maxy + NDC_TRANSFORM;

            // 유효성 검사
            if (minx >= maxx || miny >= maxy) return true;                              // AABB 유효성 검사
            if (maxx < 0.0f || minx > 1.0f || maxy < 0.0f || miny > 1.0f) return true;  // 뷰포트 검사
            if (_zbuffer == null || _zbuffer.Count == 0) return true;                   // 버퍼 검사

            // 스크린 공간 영역 계산(s=start, e=end)
            int sx = Math.Max(0, (int)(minx * _width) - EXTRA_PADDING);
            int sy = Math.Max(0, (int)(miny * _height) - EXTRA_PADDING);
            int ex = Math.Min(_width - 1, (int)(maxx * _width) + EXTRA_PADDING);
            int ey = Math.Min(_height - 1, (int)(maxy * _height) + EXTRA_PADDING);

            // LOD 레벨 계산
            int sizeX = ex - sx;
            int sizeY = ey - sy;
            int searchLOD = CalculateLODLevel(sizeX, sizeY);

            // 각 레벨별 깊이값 검사 (coarse-to-fine)
            for (int level = searchLOD; level >= 0; level--)
            {
                float[] depthBuffer = _zbuffer[level];
                int levelWidth = _width >> level;
                int levelHeight = _height >> level;

                // AABB 영역 계산 (패딩 포함)
                int startX = Math.Max(0, (int)(minx * levelWidth) - EXTRA_PADDING);
                int startY = Math.Max(0, (int)(miny * levelHeight) - EXTRA_PADDING);
                int endX = Math.Min(levelWidth - 1, (int)(maxx * levelWidth) + EXTRA_PADDING);
                int endY = Math.Min(levelHeight - 1, (int)(maxy * levelHeight) + EXTRA_PADDING);

                bool allOccluded = true;

                // AABB 영역 내 픽셀 검사
                for (int y = startY; y <= endY && allOccluded; y++)
                {
                    for (int x = startX; x <= endX; x++)
                    {
                        int index = y * levelWidth + x;
                        float terrainDepth = depthBuffer[index];

                        if (minz <= terrainDepth)
                        {
                            allOccluded = false;
                            break;
                        }
                    }
                }

                if (allOccluded) return true;
            }

            return false;
        }

        /// <summary>
        /// 주어진 레벨의 텍스처 ID를 반환합니다.
        /// </summary>
        /// <param name="level">요청할 깊이 맵 레벨</param>
        /// <returns>해당 레벨의 텍스처 ID</returns>
        /// <exception cref="ArgumentOutOfRangeException">유효하지 않은 레벨이 입력된 경우</exception>
        public uint GetTexture(int level)
        {
            if (level < 0 || level >= Levels)
                throw new ArgumentOutOfRangeException("Invalid HZB level");
            return _hzbTextures[level];
        }

        /// <summary>
        /// 현재 프레임버퍼를 HierarchicalZBuffer의 FBO로 바인딩합니다.
        /// </summary>
        public void FrameBind()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        }

        /// <summary>
        /// HierarchicalZBuffer의 FBO에서 해제한다.
        /// </summary>
        public void FrameUnbind()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        /// <summary>
        /// OpenGL 뷰포트를 설정하고 렌더링 깊이 표면을 초기화합니다
        /// </summary>
        public void PrepareRenderSurface()
        {
            Gl.Viewport(0, 0, _width, _height);
            Gl.Clear(ClearBufferMask.DepthBufferBit);
        }

        /// <summary>
        /// 메모리를 해제한다.
        /// </summary>
        public void Dispose()
        {
            Gl.DeleteFramebuffers(1, _fbo);
            Gl.DeleteTextures(_hzbTextures);
            Gl.DeleteTextures(1, _depthTexture);
            Gl.DeleteTextures(1, _colorTexture);
        }
    }

}

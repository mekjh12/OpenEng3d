using Common.Abstractions;
using Geometry;
using Model3d;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.IO;
using ZetaExt;

namespace Occlusion
{
    /// <summary>
    /// 계층적 Z-버퍼를 구현한 클래스입니다.
    /// 깊이 맵의 다양한 해상도 레벨을 관리하여 occlusion culling을 수행합니다.
    /// 참고문헌: Hierarchical-Z map based occlusion culling
    /// </summary>
    public abstract class HierarchicalAbstracZBuffer
    {
        // ===================================================================
        // 상수 정의
        // ===================================================================

        protected const int POSITION_ATTRIB = 0;    // 위치 속성 위치
        protected const int TEXCOORD_ATTRIB = 1;    // 텍스처 좌표 속성 위치
        protected const int PATCH_VERTICES = 4;     // 패치 정점 수


        // ===================================================================
        // 필드 변수
        // ===================================================================

        // OpenGL 리소스
        protected uint _fbo;                        // FBO
        protected uint[] _hzbTextures;              // 계층별 텍스처
        protected uint _depthTexture;               // 깊이 텍스처
        protected uint _colorTexture;               // 색상 텍스처

        // Z-버퍼 속성
        protected int _levels;                      // 계층 수
        protected int _width;                       // Z-Buffer 초기 너비
        protected int _height;                      // Z-Buffer 초기 높이
        protected List<float[]> _zbuffer;           // CPU 측 Z-버퍼 배열

        // 셰이더
        protected TerrainDepthShader _terrainDepthShader;     // 간단지형 쉐이더
        protected HzmMipmapShader _mipmapShader;            // 밉맵쉐이더

        // ===================================================================
        // 속성
        // ===================================================================

        public uint Framebuffer => _fbo;
        public uint DepthTexture => _depthTexture;
        public int Levels => _levels;
        public int Width => _width;
        public int Height => _height;
        public List<float[]> Zbuffer => _zbuffer;


        // ===================================================================
        // 생성자 및 초기화
        // ===================================================================

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
        public HierarchicalAbstracZBuffer(int width, int height, string projectPath)
        {
            // 기본 속성 초기화
            _width = width;
            _height = height;
            _levels = (int)Math.Floor(Math.Log(Math.Max(width, height), 2)) + 1;
            _hzbTextures = new uint[_levels];

            // 버퍼 생성
            CreateFramebufferAndTextures();

            // 셰이더 초기화
            if (_terrainDepthShader == null) _terrainDepthShader = new TerrainDepthShader(projectPath);
            if (_mipmapShader == null) _mipmapShader = new HzmMipmapShader(projectPath);
        }

        /// <summary>
        /// 프레임버퍼와 관련 텍스처를 생성합니다.
        /// </summary>
        private void CreateFramebufferAndTextures()
        {
            // 프레임버퍼 생성
            _fbo = Gl.GenFramebuffer();

            // 컬러 텍스처 초기화
            _colorTexture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _colorTexture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0,
                InternalFormat.R32f,  // ✅ (InternalFormat)0x822E → InternalFormat.R32f
                _width, _height, 0,
                PixelFormat.Red, PixelType.Float,  // ✅ UnsignedByte → Float
                IntPtr.Zero);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.CLAMP_TO_EDGE);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 깊이 텍스처 초기화 (32비트 부동소수점 포맷)
            _depthTexture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _depthTexture);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent32f, 
                _width, _height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
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

                Gl.TexImage2D(TextureTarget.Texture2d, 0,
                    InternalFormat.R32f,
                    _width >> i, _height >> i, 0,
                    PixelFormat.Red, PixelType.Float, IntPtr.Zero);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, Gl.NEAREST);
                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, Gl.NEAREST);

                // ✅ 생성 직후 검증
                int actualFormat = 0;
                int width = 0;
                int height = 0;
                Gl.GetTexLevelParameter(TextureTarget.Texture2d, 0, GetTextureParameter.TextureInternalFormat, out actualFormat);
                Gl.GetTexLevelParameter(TextureTarget.Texture2d, 0, GetTextureParameter.TextureWidth, out width);
                Gl.GetTexLevelParameter(TextureTarget.Texture2d, 0, GetTextureParameter.TextureHeight, out height);
                Console.WriteLine($"HZB Texture {i}: Format = 0x{actualFormat:X} (예상: 0x{(int)InternalFormat.R32f:X}) wxh={width}x{height}, ");
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

        // ===================================================================
        // 계층적 Z-버퍼 생성
        // ===================================================================

        /// <summary>
        /// 밉맵쉐이더를 이용하여 GPU에서 계층적 깊이 맵을 생성합니다.
        /// <code>
        /// 처리 과정:
        /// 1. 밉맵 쉐이더 바인딩
        /// 2. 각 레벨별로 다음 작업 수행:
        ///    - 프레임버퍼에 현재 레벨의 텍스처 연결
        ///    - 뷰포트 크기 설정 (레벨에 따라 1/2씩 감소)
        ///    - 이전 레벨의 깊이 텍스처 로드 (0레벨은 기본 깊이 텍스처 사용)
        ///    - 현재 레벨의 크기 정보를 쉐이더에 전달
        ///    - 깊이 테스트 비활성화 후 렌더링
        /// 3. 프레임버퍼를 원래 상태로 복원
        /// </code>
        /// </summary>
        /// <param name="maxDepth">생성할 최대 레벨 깊이. 0부터 시작하여 maxDepth-1 레벨까지 생성됩니다.</param>
        protected void GenerateHierachyZBufferOnGPU(int maxDepth = -1)
        {
            // 최대 레벨까지 생성한다.
            if (maxDepth == -1) maxDepth = _levels - 1;

            _mipmapShader.Bind();
            for (int i = 0; i <= maxDepth; i++)
            {
                Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, _hzbTextures[i], 0);
                Gl.Viewport(0, 0, _width >> i, _height >> i);
                _mipmapShader.LoadDepthBuffer(TextureUnit.Texture0, i == 0 ? _depthTexture : _hzbTextures[i - 1]);
                _mipmapShader.LoadLastMipSize(new Vertex2i(_width >> i, _height >> i));
                Gl.Disable(EnableCap.DepthTest);
                Gl.DrawArrays(PrimitiveType.Points, 0, 1);
                Gl.Enable(EnableCap.DepthTest);
            }
            _mipmapShader.Unbind();

            // 프레임버퍼 상태 복원
            Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, 
                TextureTarget.Texture2d, _colorTexture, 0);
        }

        /// <summary>
        /// GPU에서 CPU로 모든 레벨의 깊이 맵 데이터를 읽어옵니다.
        /// </summary>
        /// <param name="maxDepth">생성할 최대 레벨 깊이. 0부터 시작하여 maxDepth-1 레벨까지 생성됩니다.</param>
        protected void TransferDepthDataToCPU(int maxDepth = -1)
        {
            // 최대 레벨까지 생성한다.
            if (maxDepth == -1) maxDepth = _levels - 1;

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);

            // 계층별 텍스처 생성
            for (int i = 0; i <= maxDepth; i++)
            {
                // 현재 프레임 버퍼에서 깊이 데이터 읽기
                int w = _width >> i;
                int h = _height >> i;

                // 텍스처에서 직접 읽기
                Gl.GetTextureImage(_hzbTextures[i], 0, PixelFormat.Red, PixelType.Float,
                    _zbuffer[i].Length * sizeof(float), _zbuffer[i]);
            }

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        // ===================================================================
        // Occlusion Culling 테스트
        // ===================================================================
        AABB3f _trans;
        /// <summary>
        /// AABB가 시야에 보이는지 검사합니다.
        /// </summary>
        /// <param name="vp">뷰프로젝션행렬</param>
        /// <param name="view">뷰행렬</param>
        /// <param name="aabb">검사할 AABB</param>
        /// <returns>AABB가 보이면 true, 가려졌으면 false</returns>
        public bool TestVisibility(Matrix4x4f vp, Matrix4x4f view, AABB3f aabb)
        {
            aabb.TransformViewSpace(vp, view, ref _trans);
            return !TestOcclusion(_trans.Min.x, _trans.Min.y, _trans.Min.z, _trans.Max.x, _trans.Max.y, _trans.Max.z);
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
        /// <returns>AABB가 완전히 가려졌으면 true, 아니면 false를 반환한다</returns>
        private bool TestOcclusion(float minx, float miny, float minz, float maxx, float maxy, float maxz)
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
            if (Zbuffer == null || Zbuffer.Count == 0) return true;                   // 버퍼 검사

            // 스크린 공간 영역 계산(s=start, e=end)
            int sx = Math.Max(0, (int)(minx * _width) - EXTRA_PADDING);
            int sy = Math.Max(0, (int)(miny * _height) - EXTRA_PADDING);
            int ex = Math.Min(_width - 1, (int)(maxx * _width) + EXTRA_PADDING);
            int ey = Math.Min(_height - 1, (int)(maxy * _height) + EXTRA_PADDING);

            // LOD 레벨 계산
            int sizeX = ex - sx;
            int sizeY = ey - sy;
            int searchLOD = ComputeLODLevel(sizeX, sizeY);

            // 각 레벨별 깊이값 검사 (coarse-to-fine)
            for (int level = searchLOD; level >= 0; level--)
            {
                float[] depthBuffer = Zbuffer[level];
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
        /// 스크린 공간 크기를 기반으로 적절한 LOD 레벨을 계산합니다.
        /// LOD 레벨부터 시작해서 레벨 0까지 계층적으로 깊이값을 검사합니다
        /// 높은 LOD 레벨(coarse)에서 낮은 레벨(fine)로 검사함으로써 성능을 최적화합니다
        /// </summary>
        /// <param name="width">스크린 공간(0-1)에서의 가로 크기</param>
        /// <param name="height">스크린 공간(0-1)에서의 세로 크기</param>
        /// <returns>계산된 LOD 레벨</returns>
        private int ComputeLODLevel(int width, int height)
        {
            return (int)Math.Ceiling(MathF.Log2(Math.Max(width, height) * 0.5f));
        }


        // ===================================================================
        // 프레임버퍼 관리
        // ===================================================================

        /// <summary>
        /// 현재 프레임버퍼를 HierarchicalZBuffer의 FBO로 바인딩합니다.
        /// </summary>
        public void BindFramebuffer()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        }

        /// <summary>
        /// HierarchicalZBuffer의 FBO에서 해제한다.
        /// </summary>
        public void UnbindFramebuffer()
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


        // ===================================================================
        // 유틸리티 메서드
        // ===================================================================

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
        /// 지형을 간단하게 그려 지평선 깊이맵을 생성합니다.
        /// 
        /// 주의: 지형 렌더링시 오클루더로 나무와 같은 작은 오브젝트 사용은 권장하지 않습니다.
        /// 깜빡임 현상이 발생할 수 있습니다.
        /// </summary>
        /// <param name="terrianPatchEntity">지형 패치</param>
        /// <param name="proj">투영 행렬</param>
        /// <param name="view">뷰 행렬</param>
        /// <param name="heightScale">높이 스케일</param>
        public void RenderSimpleTerrain(Entity terrianPatchEntity, Matrix4x4f proj, Matrix4x4f view, float heightScale)
        {
            if (terrianPatchEntity == null) return;
            if (terrianPatchEntity?.Model == null || terrianPatchEntity.Model.Length == 0)
                throw new ArgumentNullException(nameof(terrianPatchEntity));

            TerrainDepthShader shader = _terrainDepthShader;
            TexturedModel terrainModel = terrianPatchEntity.Model[0] as TexturedModel;

            shader.Bind();
            shader.LoadProjectionMatrix(proj);
            shader.LoadViewMatrix(view);
            shader.LoadModelMatrix(terrianPatchEntity.ModelMatrix);
            shader.LoadHeightScale(heightScale);
            shader.LoadHeightMap(TextureUnit.Texture0, terrainModel.Texture == null ? 0 : terrainModel.Texture.TextureID);

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
                Gl.DisableVertexAttribArray(TEXCOORD_ATTRIB);
                Gl.DisableVertexAttribArray(POSITION_ATTRIB);
                Gl.BindVertexArray(0);
                shader.Unbind();
            }
        }

        public void SaveTextureToImageWithColorMap(int level, string filepath)
        {
            if (!Directory.Exists(filepath)) 
                Directory.CreateDirectory(Path.GetDirectoryName(filepath));

            int levelWidth = _width >> level;
            int levelHeight = _height >> level;
            float[] data = new float[levelWidth * levelHeight];

            // GPU에서 데이터 읽기
            Gl.BindTexture(TextureTarget.Texture2d, _hzbTextures[level]);
            Gl.GetTexImage(TextureTarget.Texture2d, 0, PixelFormat.Red, PixelType.Float, data);
            Gl.BindTexture(TextureTarget.Texture2d, 0);

            // 최대값 찾기
            float maxVal = 0;
            for (int i = 0; i < data.Length; i++)
            {
                maxVal = Math.Max(maxVal, data[i]);
            }

            Console.WriteLine($"Level {level} 최대값: {maxVal}");

            using (var bitmap = new System.Drawing.Bitmap(levelWidth, levelHeight,
                   System.Drawing.Imaging.PixelFormat.Format24bppRgb))
            {
                for (int y = 0; y < levelHeight; y++)
                {
                    for (int x = 0; x < levelWidth; x++)
                    {
                        // ✅ Y축 반전: OpenGL은 아래에서 위로, Bitmap은 위에서 아래로
                        int flippedY = levelHeight - 1 - y;
                        float value = data[flippedY * levelWidth + x];
                        float normalized = maxVal > 0 ? value / maxVal : 0;

                        // Jet 컬러맵 (파랑 → 초록 → 빨강)
                        System.Drawing.Color color;
                        if (normalized < 0.25f)
                        {
                            float t = normalized * 4;
                            color = System.Drawing.Color.FromArgb(0, (int)(t * 255), 255);
                        }
                        else if (normalized < 0.5f)
                        {
                            float t = (normalized - 0.25f) * 4;
                            color = System.Drawing.Color.FromArgb(0, 255, (int)((1 - t) * 255));
                        }
                        else if (normalized < 0.75f)
                        {
                            float t = (normalized - 0.5f) * 4;
                            color = System.Drawing.Color.FromArgb((int)(t * 255), 255, 0);
                        }
                        else
                        {
                            float t = (normalized - 0.75f) * 4;
                            color = System.Drawing.Color.FromArgb(255, (int)((1 - t) * 255), 0);
                        }

                        bitmap.SetPixel(x, y, color);
                    }
                }

                bitmap.Save(filepath, System.Drawing.Imaging.ImageFormat.Png);
            }

            Console.WriteLine($"컬러맵 이미지 저장됨: {filepath}");
        }

        /// <summary>
        /// 깊이 버퍼를 화면에 렌더링합니다 (디버깅용).
        /// </summary>
        /// <param name="shader">깊이 시각화 셰이더</param>
        /// <param name="camera">카메라 정보</param>
        /// <param name="level">표시할 밉맵 레벨</param>
        public void RenderDepthBuffer(HzmDepthShader shader, Camera camera, int level)
        {
            // ✅ 바인딩 전 상태 초기화
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            shader.Bind();
            shader.LoadCameraFar(camera.FAR);
            shader.LoadCameraNear(camera.NEAR);
            shader.LoadIsPerspective(false);
            shader.LoadLOD(level);

            // ✅ 텍스처 바인딩 전 활성화
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, _hzbTextures[level]);
            shader.LoadDepthTexture(TextureUnit.Texture0, _hzbTextures[level]);

            Gl.Disable(EnableCap.DepthTest);
            Gl.DrawArrays(PrimitiveType.Points, 0, 1);
            Gl.Enable(EnableCap.DepthTest);

            // ✅ 언바인딩
            Gl.BindTexture(TextureTarget.Texture2d, 0);
            shader.Unbind();
        }


        // ===================================================================
        // 리소스 해제
        // ===================================================================

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
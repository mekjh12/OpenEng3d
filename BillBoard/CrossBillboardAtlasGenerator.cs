using Common;
using Common.Abstractions;
using Model3d;
using OpenGL;
using Renderer;
using Shader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace BillBoard
{
    /// <summary>
    /// Billboard Cloud용 Atlas 텍스처 생성기
    /// ImpostorAtlasGenerator 패턴을 따라 6개 평면(수직 4개 + 수평 2개)을 렌더링
    /// <code>
    /// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    /// 📖 읽기 가이드
    /// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    /// 
    /// [진입점]
    ///   GenerateAtlas()
    /// 
    /// [초기화 흐름]
    ///   GenerateAtlas()
    ///   ├─► InitializeRenderTarget()  : _atlasRenderTarget 생성
    ///   ├─► CalculateModelBounds()    : 모델 경계 계산
    ///   ├─► CalculatePlaneData()      : 6개 평면 정보 생성
    ///   └─► RenderAtlas()             : 실제 렌더링 수행
    /// 
    /// [Atlas 레이아웃] 1024x512
    ///   ┌────────┬────────┬────────┬────────┐
    ///   │  0°    │  45°   │  90°   │  135°  │ ← 256x256 each (수직)
    ///   ├────────┼────────┼────────┼────────┤
    ///   │  Top   │ Bottom │        │        │ ← 256x256 each (수평)
    ///   └────────┴────────┴────────┴────────┘
    /// 
    /// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    /// </code>
    /// </summary>
    public class CrossBillboardAtlasGenerator : IDisposable
    {
        // === 설정 ===
        public const int AtlasWidth = 1024;
        public const int AtlasHeight = 256;
        public const int PlaneSize = 256;  // 모든 평면이 256x256

        /// <summary>
        /// 각 평면의 렌더링 정보
        /// </summary>
        private class PlaneData
        {
            public Matrix4x4f ViewMatrix;      // 뷰 행렬
            public Matrix4x4f ProjectionMatrix; // 투영 행렬
            public Vertex2f AtlasOffset;       // Atlas UV 오프셋
            public string DebugName;           // 디버그 이름
        }

        private List<PlaneData> _planeDataList;
        private RenderTarget2D _atlasRenderTarget;
        private UnifiedModelRenderer _unifiedModelRenderer;

        public CrossBillboardAtlasGenerator()
        {
            _planeDataList = new List<PlaneData>();
        }

        /// <summary>
        /// 렌더 타겟 초기화
        /// </summary>
        private void InitializeRenderTarget()
        {
            _atlasRenderTarget = new RenderTarget2D(
                AtlasWidth,
                AtlasHeight,
                false,
                SurfaceFormat.Color,
                DepthFormat.Depth24
            );
        }

        /// <summary>
        /// Atlas 생성 메인 함수
        /// </summary>
        public CrossBillboardData GenerateAtlas(UnlitShader shader, UnifiedTexturedModel model)
        {
            // 통합 모델 렌더러 생성
            _unifiedModelRenderer = new UnifiedModelRenderer(model, shader);

            // 렌더 타겟 준비
            InitializeRenderTarget();

            // 약간의 패딩된 모델 바운딩박스 계산
            AABB3f bounds = CalculateModelBounds(model.AABB);

            // 원점이 바닥 중앙에 오도록 변환
            //bounds = CreateAABB3fPivotOriginalPoint(bounds);

            // 3개 수직 평면 데이터 계산
            CalculatePlaneData(bounds);

            // 렌더링
            RenderAtlas(shader, model);

            // BillboardCloudData 생성
            var data = CreateBillboardCloudData(model, bounds);

            // 디버그 저장
            GetAtlasTexture(true).Save($@"C:\Users\mekjh\OneDrive\바탕 화면\{model.Name}_billboardcloud.png");

            return data;
        }

        /// <summary>
        /// 모델의 원점이 AABB의 바닥 중앙에 오도록 AABB3f를 수정한다.
        /// </summary>
        /// <param name="aabb"></param>
        /// <returns></returns>
        private AABB3f CreateAABB3fPivotOriginalPoint(AABB3f aabb)
        {
            // 원점이 AABB의 바닥 중앙에 오도록 변환
            float maxX = Math.Max(Math.Abs(aabb.Min.x), Math.Abs(aabb.Max.x));
            float maxY = Math.Max(Math.Abs(aabb.Min.y), Math.Abs(aabb.Max.y));
            float maxZ = aabb.Max.z;
            float minZ = aabb.Min.z;

            // 새 AABB3f 생성
            return new AABB3f(
                new Vertex3f(-maxX, -maxY, minZ),
                new Vertex3f(maxX, maxY, maxZ)
            );
        }

        /// <summary>
        /// 모델 바운딩 박스 계산 (약간의 패딩 추가)
        /// </summary>
        private AABB3f CalculateModelBounds(AABB3f modelAABB, float padding = 0.01f)
        {
            float PADDING_FACTOR = 1f + padding;
            Vertex3f center = modelAABB.Center;
            Vertex3f extents = modelAABB.Size * (PADDING_FACTOR * 0.5f);

            return new AABB3f(
                center - extents,
                center + extents
            );
        }

        /// <summary>
        /// 6개 평면의 렌더링 데이터 계산
        /// ✅ 수직 평면의 아래쪽 중간 = 모델 원점(바닥 중심)
        /// </summary>
        private void CalculatePlaneData(AABB3f bounds)
        {
            _planeDataList.Clear();

            // ✅ 바닥 중심을 원점으로
            Vertex3f bottomCenter = new Vertex3f(
                bounds.Center.x,
                bounds.Center.y,
                bounds.Min.z  // 바닥
            );

            float radius = Math.Max(Math.Max(bounds.Size.x, bounds.Size.y), bounds.Size.z) * 0.5f;
            float objectHeight = bounds.Size.z;
            float cameraDistance = radius * 3.0f;

            // 직교 투영 행렬 (공통)
            float orthoSize = radius;
            Matrix4x4f orthoProj = Matrix4x4f.Ortho(
                -orthoSize, orthoSize,
                -orthoSize, orthoSize,
                0.1f, cameraDistance * 2.0f
            );

            // === 수직 평면 3 (0°, 60°, 120°) ===
            float[] angles = CrossBillboardAtlasLayout.VerticalAngles;
            for (int i = 0; i < 3; i++)
            {
                float angleRad = angles[i] * (float)Math.PI / 180f;

                // ✅ 카메라는 중간 높이에 배치
                Vertex3f cameraPos = new Vertex3f(
                    bottomCenter.x + cameraDistance * (float)Math.Cos(angleRad),
                    bottomCenter.y + cameraDistance * (float)Math.Sin(angleRad),
                    bottomCenter.z + objectHeight * 0.5f  // 바닥 + 절반 높이
                );

                // ✅ 중간 높이를 바라봄
                Vertex3f lookAtTarget = new Vertex3f(
                    bottomCenter.x,
                    bottomCenter.y,
                    bottomCenter.z + objectHeight * 0.5f
                );

                PlaneData plane = new PlaneData
                {
                    ViewMatrix = Matrix4x4f.LookAt(cameraPos, lookAtTarget, Vertex3f.UnitZ),
                    ProjectionMatrix = orthoProj,
                    AtlasOffset = new Vertex2f(i * PlaneSize / (float)AtlasWidth, 0f),
                    DebugName = $"Vertical_{angles[i]}°"
                };

                _planeDataList.Add(plane);
            }
        }

        /// <summary>
        /// 실제 Atlas 렌더링
        /// </summary>
        private void RenderAtlas(UnlitShader shader, UnifiedTexturedModel model)
        {
            // 렌더 타겟 바인딩
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasRenderTarget.FrameBuffer);
            Gl.ClearColor(0, 0, 0, 0);  // 투명 배경
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.Enable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // 각 평면 렌더링
            foreach (PlaneData plane in _planeDataList)
            {
                // Viewport 설정
                Gl.Viewport(
                    (int)(AtlasWidth * plane.AtlasOffset.x),
                    (int)(AtlasHeight * plane.AtlasOffset.y),
                    PlaneSize,
                    PlaneSize
                );

                shader.Bind();

                // MVP 계산
                Matrix4x4f mvp = plane.ProjectionMatrix * plane.ViewMatrix;

                // 렌더링
                _unifiedModelRenderer.Render(mvp, plane.ViewMatrix);

                shader.Unbind();
            }

            Gl.Disable(EnableCap.Blend);
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        private CrossBillboardData CreateBillboardCloudData(UnifiedTexturedModel model, AABB3f bounds)
        {
            CrossBillboardData data = new CrossBillboardData();

            // ✅ 바닥 중심 기준으로 경계 정보 저장
            Vertex3f bottomCenter = new Vertex3f(
                bounds.Center.x,
                bounds.Center.y,
                bounds.Min.z
            );

            data.BoundsMin = new Vertex3f(bounds.Min.x, bounds.Min.y, bounds.Min.z);
            data.BoundsMax = new Vertex3f(bounds.Max.x, bounds.Max.y, bounds.Max.z);
            data.ObjectWidth = Math.Max(bounds.Size.x, bounds.Size.y);
            data.ObjectHeight = bounds.Size.z;  // Z축이 높이

            // ✅ 원점 오프셋 정보 추가 (선택사항)
            //data.PivotOffset = new Vertex3f(0, 0, 0);  // 바닥 중심이 원점

            // UV 영역
            data.Regions = CrossBillboardAtlasLayout.CalculateRegions();

            // Atlas 텍스처
            data.AtlasTexture = new Texture(_atlasRenderTarget.TextureHandle, AtlasWidth, AtlasHeight);
            data.AtlasWidth = AtlasWidth;
            data.AtlasHeight = AtlasHeight;

            return data;
        }

        /// <summary>
        /// Atlas를 Bitmap으로 반환
        /// </summary>
        public Bitmap GetAtlasTexture(bool drawBorders = false)
        {
            IntPtr pixelsPtr = IntPtr.Zero;

            try
            {
                int size = AtlasWidth * AtlasHeight * 4;
                pixelsPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

                Gl.GetInteger(GetPName.ReadFramebufferBinding, out uint previousFramebuffer);
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasRenderTarget.FrameBuffer);

                Gl.ReadPixels(0, 0, AtlasWidth, AtlasHeight,
                    OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, pixelsPtr);

                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer);

                byte[] pixels = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(pixelsPtr, pixels, 0, size);

                Bitmap bitmap = new Bitmap(AtlasWidth, AtlasHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                try
                {
                    unsafe
                    {
                        byte* bitmapPtr = (byte*)bitmapData.Scan0;
                        int stride = bitmapData.Stride;

                        for (int y = 0; y < AtlasHeight; y++)
                        {
                            for (int x = 0; x < AtlasWidth; x++)
                            {
                                int srcIndex = (((AtlasHeight - 1 - y) * AtlasWidth) + x) * 4;
                                int dstIndex = (y * stride) + (x * 4);

                                // RGBA → BGRA
                                bitmapPtr[dstIndex + 0] = pixels[srcIndex + 2]; // B
                                bitmapPtr[dstIndex + 1] = pixels[srcIndex + 1]; // G
                                bitmapPtr[dstIndex + 2] = pixels[srcIndex + 0]; // R
                                bitmapPtr[dstIndex + 3] = pixels[srcIndex + 3]; // A
                            }
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                // 테두리 그리기
                if (drawBorders)
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        using (Pen pen = new Pen(Color.FromArgb(255, Color.Red), 2))
                        {
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                            // 6개 평면 테두리
                            for (int i = 0; i < 6; i++)
                            {
                                int x = (i % 4) * PlaneSize;
                                int y = (i / 4) * PlaneSize;
                                g.DrawRectangle(pen, x + 1, y + 1, PlaneSize - 3, PlaneSize - 3);
                            }
                        }
                    }
                }

                return bitmap;
            }
            finally
            {
                if (pixelsPtr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(pixelsPtr);
                }
            }
        }

        public void Dispose()
        {
            _atlasRenderTarget?.Dispose();
        }
    }
}
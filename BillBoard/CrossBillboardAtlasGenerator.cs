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
using ZetaExt;

namespace BillBoard
{
    public class CrossBillboardAtlasGenerator : IDisposable
    {
        // === 설정 ===
        public const int AtlasWidth = 1024;
        public const int AtlasHeight = 256;
        public const int PlaneSize = 256;

        private class PlaneData
        {
            public Matrix4x4f ViewMatrix;
            public Matrix4x4f ProjectionMatrix;
            public Vertex2f AtlasOffset;
            public string DebugName;
        }

        private List<PlaneData> _planeDataList;

        // ✅ 텍스처 핸들 직접 저장
        private uint _colorTexture;
        private uint _normalTexture;
        private uint _depthRenderBuffer;
        private uint _atlasFBO;

        private UnifiedModelRenderer _unifiedModelRenderer;

        public CrossBillboardAtlasGenerator()
        {
            _planeDataList = new List<PlaneData>();
        }

        private void InitializeRenderTarget()
        {
            // Depth Renderbuffer 생성
            _depthRenderBuffer = Gl.GenRenderbuffer();
            Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depthRenderBuffer);
            Gl.RenderbufferStorage(
                RenderbufferTarget.Renderbuffer,
                InternalFormat.Depth24Stencil8,
                AtlasWidth,
                AtlasHeight
            );

            // ✅ Color 텍스처 생성 (필드에 저장)
            _colorTexture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _colorTexture);
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.Rgba8,
                AtlasWidth,
                AtlasHeight,
                0,
                OpenGL.PixelFormat.Rgba,
                PixelType.UnsignedByte,
                IntPtr.Zero
            );
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // ✅ Normal 텍스처 생성 (필드에 저장)
            _normalTexture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, _normalTexture);
            Gl.TexImage2D(
                TextureTarget.Texture2d,
                0,
                InternalFormat.Rgba8,
                AtlasWidth,
                AtlasHeight,
                0,
                OpenGL.PixelFormat.Rgba,
                PixelType.UnsignedByte,
                IntPtr.Zero
            );
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // MRT용 FBO 생성
            _atlasFBO = Gl.GenFramebuffer();
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasFBO);

            // Color attachment (location = 0)
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2d,
                _colorTexture,
                0
            );

            // Normal attachment (location = 1)
            Gl.FramebufferTexture2D(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment1,
                TextureTarget.Texture2d,
                _normalTexture,
                0
            );

            // Depth attachment
            Gl.FramebufferRenderbuffer(
                FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthStencilAttachment,
                RenderbufferTarget.Renderbuffer,
                _depthRenderBuffer
            );

            // ✅ MRT 활성화
            Gl.DrawBuffers(new int[] { Gl.COLOR_ATTACHMENT0, Gl.COLOR_ATTACHMENT1 });

            // FBO 상태 체크
            FramebufferStatus status = Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferStatus.FramebufferComplete)
            {
                throw new Exception($"Framebuffer incomplete: {status}");
            }

            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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
            var data = CreateCrossBillboardData(model, bounds);

            // 디버그 저장
            GetAtlasTexture(true).Save($@"C:\Users\mekjh\OneDrive\바탕 화면\{model.Name}_cb.png");
            //GetAtlasDepthTexture(true).Save($@"C:\Users\mekjh\OneDrive\바탕 화면\{model.Name}_cb_depth.png");
            //GetAtlasNormalTexture(true).Save($@"C:\Users\mekjh\OneDrive\바탕 화면\{model.Name}_normal.png");

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
        /// <summary>
        /// 3개 평면의 렌더링 데이터 계산
        /// ✅ 물체의 종횡비를 고려한 직교 투영
        /// </summary>
        private void CalculatePlaneData(AABB3f bounds)
        {
            _planeDataList.Clear();

            Vertex3f bottomCenter = new Vertex3f(
                bounds.Center.x,
                bounds.Center.y,
                bounds.Min.z
            );

            // ✅ 수직 평면의 실제 크기 계산
            float horizontalRadius = Math.Max(bounds.Size.x, bounds.Size.y) * 0.5f;
            float verticalRadius = bounds.Size.z * 0.5f;
            float cameraDistance = Math.Max(horizontalRadius, verticalRadius) * 3.0f;

            // ✅ 물체 비율에 맞는 직교 투영 (수직 평면용)
            // 가로는 XY 평면의 반경, 세로는 높이
            Matrix4x4f verticalOrthoProj = Matrix4x4f.Ortho(
                -horizontalRadius, horizontalRadius,      // 좌우: XY 평면 크기
                -verticalRadius, verticalRadius,          // 상하: Z 높이
                0.1f, cameraDistance * 2.0f
            );

            float objectHeight = bounds.Size.z;
            float[] angles = CrossBillboardAtlasLayout.VerticalAngles;

            for (int i = 0; i < 3; i++)
            {
                float angleRad = angles[i] * (float)Math.PI / 180f;

                Vertex3f cameraPos = new Vertex3f(
                    bottomCenter.x + cameraDistance * (float)Math.Cos(angleRad),
                    bottomCenter.y + cameraDistance * (float)Math.Sin(angleRad),
                    bottomCenter.z + objectHeight * 0.5f
                );

                Vertex3f lookAtTarget = new Vertex3f(
                    bottomCenter.x,
                    bottomCenter.y,
                    bottomCenter.z + objectHeight * 0.5f
                );

                PlaneData plane = new PlaneData
                {
                    ViewMatrix = Matrix4x4f.LookAt(cameraPos, lookAtTarget, Vertex3f.UnitZ),
                    ProjectionMatrix = verticalOrthoProj,  // ✅ 비율에 맞는 투영
                    AtlasOffset = new Vertex2f(i * PlaneSize / (float)AtlasWidth, 0f),
                    DebugName = $"Vertical_{angles[i]}°"
                };

                _planeDataList.Add(plane);
            }
        }

        private void RenderAtlas(UnlitShader shader, UnifiedTexturedModel model)
        {
            // MRT 렌더 타겟 바인딩
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasFBO);

            // 두 버퍼 모두 클리어
            Gl.ClearColor(0, 0, 0, 0);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            // Normal 버퍼 기본값 설정
            Gl.DrawBuffers(new int[] { Gl.COLOR_ATTACHMENT1 });
            Gl.ClearColor(0.5f, 0.5f, 1.0f, 0.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            // MRT 모드 복원
            Gl.DrawBuffers(new int[] { Gl.COLOR_ATTACHMENT0, Gl.COLOR_ATTACHMENT1 });

            Gl.Enable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);
            Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            foreach (PlaneData plane in _planeDataList)
            {
                Gl.Viewport(
                    (int)(AtlasWidth * plane.AtlasOffset.x),
                    (int)(AtlasHeight * plane.AtlasOffset.y),
                    PlaneSize,
                    PlaneSize
                );

                shader.Bind();

                Matrix4x4f mvp = plane.ProjectionMatrix * plane.ViewMatrix;
                Matrix4x4f mv = plane.ViewMatrix;

                // ✅ Normal Matrix 계산 (View의 3x3 부분의 inverse transpose)
                // uniform scale이면 단순히 View의 3x3 부분 사용 가능
                Matrix3x3f normalMatrix = GetNormalMatrix(plane.ViewMatrix);

                shader.LoadMVPMatrix(mvp);
                shader.LoadModelView(mv);
                shader.LoadNormalMatrix(normalMatrix);

                _unifiedModelRenderer.Render(mvp, mv);

                shader.Unbind();
            }

            Gl.Disable(EnableCap.Blend);
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        // ✅ Normal Matrix 계산 헬퍼 함수
        private Matrix3x3f GetNormalMatrix(Matrix4x4f viewMatrix)
        {
            // View Matrix의 3x3 부분 추출
            Matrix3x3f mat3 = viewMatrix.Rot3x3f();

            // Inverse Transpose (uniform scale이면 생략 가능)
            return mat3.Transposed.Inversed();
        }

        private CrossBillboardData CreateCrossBillboardData(UnifiedTexturedModel model, AABB3f bounds)
        {
            CrossBillboardData data = new CrossBillboardData();

            Vertex3f bottomCenter = new Vertex3f(
                bounds.Center.x,
                bounds.Center.y,
                bounds.Min.z
            );

            data.BoundsMin = new Vertex3f(bounds.Min.x, bounds.Min.y, bounds.Min.z);
            data.BoundsMax = new Vertex3f(bounds.Max.x, bounds.Max.y, bounds.Max.z);
            data.ObjectWidth = Math.Max(bounds.Size.x, bounds.Size.y);
            data.ObjectHeight = bounds.Size.z;

            data.Regions = CrossBillboardAtlasLayout.CalculateRegions();

            // ✅ 직접 텍스처 핸들 사용
            data.AtlasTexture = new Texture(_colorTexture, AtlasWidth, AtlasHeight);
            data.NormalTexture = new Texture(_normalTexture, AtlasWidth, AtlasHeight);
            data.AtlasWidth = AtlasWidth;
            data.AtlasHeight = AtlasHeight;

            return data;
        }

        public Bitmap GetAtlasTexture(bool drawBorders = false)
        {
            IntPtr pixelsPtr = IntPtr.Zero;

            try
            {
                int size = AtlasWidth * AtlasHeight * 4;
                pixelsPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

                Gl.GetInteger(GetPName.ReadFramebufferBinding, out uint previousFramebuffer);

                // ✅ _atlasFBO 바인딩하고 ColorAttachment0 읽기
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasFBO);
                Gl.ReadBuffer(ReadBufferMode.ColorAttachment0);

                Gl.ReadPixels(0, 0, AtlasWidth, AtlasHeight,
                    OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, pixelsPtr);

                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer);

                byte[] pixels = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(pixelsPtr, pixels, 0, size);

                Bitmap bitmap = new Bitmap(AtlasWidth, AtlasHeight,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
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

                if (drawBorders)
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        using (Pen pen = new Pen(Color.FromArgb(255, Color.Red), 2))
                        {
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                            for (int i = 0; i < 3; i++)  // ✅ 3개 평면만
                            {
                                int x = i * PlaneSize;
                                int y = 0;
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

        public Bitmap GetAtlasNormalTexture(bool drawBorders = false)
        {
            IntPtr pixelsPtr = IntPtr.Zero;

            try
            {
                int size = AtlasWidth * AtlasHeight * 4;
                pixelsPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

                // ✅ _atlasFBO 바인딩하고 ColorAttachment1 읽기
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasFBO);
                Gl.ReadBuffer(ReadBufferMode.ColorAttachment1);

                Gl.ReadPixels(0, 0, AtlasWidth, AtlasHeight,
                    OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, pixelsPtr);

                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

                byte[] pixels = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(pixelsPtr, pixels, 0, size);

                Bitmap bitmap = new Bitmap(AtlasWidth, AtlasHeight,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);
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

                if (drawBorders)
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                        using (Pen pen = new Pen(Color.FromArgb(255, Color.Green), 2))
                        {
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                            for (int i = 0; i < 3; i++)
                            {
                                int x = i * PlaneSize;
                                int y = 0;
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

        public Bitmap GetAtlasDepthTexture(bool drawBorders = false)
        {
            IntPtr depthPtr = IntPtr.Zero;

            try
            {
                int size = AtlasWidth * AtlasHeight * sizeof(float);
                depthPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

                Gl.GetInteger(GetPName.ReadFramebufferBinding, out uint previousFramebuffer);

                // ✅ _atlasFBO 바인딩
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasFBO);

                Gl.ReadPixels(
                    0, 0,
                    AtlasWidth,
                    AtlasHeight,
                    OpenGL.PixelFormat.DepthComponent,
                    PixelType.Float,
                    depthPtr
                );

                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer);

                float[] depthValues = new float[AtlasWidth * AtlasHeight];
                System.Runtime.InteropServices.Marshal.Copy(depthPtr, depthValues, 0, depthValues.Length);

                Bitmap bitmap = new Bitmap(AtlasWidth, AtlasHeight,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                var bitmapData = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    System.Drawing.Imaging.ImageLockMode.WriteOnly,
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
                                int srcIndex = ((AtlasHeight - 1 - y) * AtlasWidth) + x;
                                int dstIndex = (y * stride) + (x * 4);

                                byte depthByte = (byte)(depthValues[srcIndex] * 255.0f);

                                bitmapPtr[dstIndex + 0] = depthByte; // B
                                bitmapPtr[dstIndex + 1] = depthByte; // G
                                bitmapPtr[dstIndex + 2] = depthByte; // R
                                bitmapPtr[dstIndex + 3] = 255;       // A
                            }
                        }
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }

                return bitmap;
            }
            finally
            {
                if (depthPtr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(depthPtr);
                }
            }
        }

        public void Dispose()
        {
            // ✅ 텍스처 정리
            if (_colorTexture != 0)
            {
                Gl.DeleteTextures(_colorTexture);
                _colorTexture = 0;
            }

            if (_normalTexture != 0)
            {
                Gl.DeleteTextures(_normalTexture);
                _normalTexture = 0;
            }

            if (_depthRenderBuffer != 0)
            {
                Gl.DeleteRenderbuffers(_depthRenderBuffer);
                _depthRenderBuffer = 0;
            }

            if (_atlasFBO != 0)
            {
                Gl.DeleteFramebuffers(_atlasFBO);
                _atlasFBO = 0;
            }
        }
    }
}
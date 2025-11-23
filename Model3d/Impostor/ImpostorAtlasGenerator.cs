using Common.Abstractions;
using Geometry;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Drawing;
using ZetaExt;

namespace Model3d
{
    /// <summary>
    /// 3D 모델의 임포스터 아틀라스를 생성하는 클래스.
    /// 임포스터는 3D 모델을 다양한 각도에서 2D 이미지로 렌더링한 것으로,
    /// 원거리에서 3D 모델 대신 사용하여 렌더링 성능을 향상시킬 수 있다.
    /// </summary>
    public class ImpostorAtlasGenerator: IDisposable
    {
        /// <summary>
        /// 각 뷰의 렌더링에 필요한 정보를 저장하는 내부 클래스
        /// </summary>
        private class ViewData
        {
            public Matrix4x4f ViewMatrix;   // 해당 각도에서의 뷰 행렬
            public Vertex2f AtlasOffset;    // 아틀라스 내에서의 UV 오프셋
            public float HorizontalAngle;   // 수평 각도 (도 단위)
            public float VerticalAngle;     // 수직 각도 (도 단위)
        }

        private List<ViewData> _viewDataList;
        private RenderTarget2D _atlasRenderTarget;  // 렌더링 결과를 저장할 렌더 타겟

        /// <summary>
        /// 임포스터 아틀라스 생성기 초기화
        /// </summary>
        /// <param name="settings">생성 설정</param>
        public ImpostorAtlasGenerator()
        {
            _viewDataList = new List<ViewData>();
        }

        /// <summary>
        /// 렌더 타겟 초기화
        /// 아틀라스 텍스처를 저장할 OpenGL 렌더 타겟을 생성한다.
        /// </summary>
        private void InitializeRenderTarget(ImpostorSettings settings)
        {
            // 알파채널을 포함한 렌더 타겟 생성
            _atlasRenderTarget = new RenderTarget2D(
                (int)settings.AtlasSize,
                (int)settings.AtlasSize,
                false,              // 밉맵 사용하지 않음
                SurfaceFormat.Color,
                DepthFormat.Depth24);
        }

        /// <summary>
        /// 아틀라스 생성 메인 함수
        /// 3D 모델들을 다양한 각도에서 렌더링하여 임포스터 아틀라스를 생성한다.
        /// </summary>
        /// <param name="shader">렌더링에 사용할 쉐이더</param>
        /// <param name="texturedModels">임포스터를 생성할 3D 모델들의 배열</param>
        /// <returns>생성된 임포스터 아틀라스의 텍스처 핸들</returns>
        public uint GenerateAtlas(UnlitShader shader, ImpostorSettings settings, string modelname, TexturedModel[] texturedModels)
        {
            // 버퍼를 준비한다.
            InitializeRenderTarget(settings);

            // 모델의 바운딩박스를 계산한다.
            AABB bounds = CalculateModelBounds(texturedModels);

            // yaw, pitch에 따른 뷰행렬리스트를 만든다.
            CalculateViewMatrices(settings, bounds);

            // 렌더링한다.
            RenderAtlas(settings, shader, texturedModels, bounds);

            // 디버깅
            GetImpostorTexture(settings, drawBorders: true).Save(@"C:\Users\mekjh\OneDrive\바탕 화면\" + modelname + ".png");
            GetImpostorDepthTexture(settings, drawBorders: true).Save(@"C:\Users\mekjh\OneDrive\바탕 화면\" + modelname + "_depth.png");

            return _atlasRenderTarget.TextureHandle;
        }

        /// <summary>
        /// 모델의 바운딩 박스 계산
        /// 모든 모델을 포함하는 최소 크기의 축 정렬 바운딩 박스(AABB)를 계산한다.
        /// </summary>
        /// <param name="texturedModels">바운딩 박스를 계산할 3D 모델들의 배열</param>
        /// <returns>모든 모델을 포함하는 AABB</returns>
        private AABB CalculateModelBounds(TexturedModel[] texturedModels)
        {
            // 모든 모델을 포함하는 AABB 계산
            AABB finalAABB = AABB.ZeroSizeAABB;
            foreach (var model in texturedModels)
            {
                finalAABB = (AABB)finalAABB.Union(model.AABB);
            }

            // 약간의 패딩 추가 (모델이 AABB 경계에 너무 딱 맞지 않도록)
            // AABB 계산 시 약간의 패딩을 추가하여 모델이 경계에 너무 딱 맞지 않도록 함
            const float PADDING_FACTOR = 1.01f;  // 1% 패딩
            Vertex3f center = finalAABB.Center;
            Vertex3f extents = finalAABB.Size * (PADDING_FACTOR * 0.5f);

            return new AABB(
                center - extents,  // min
                center + extents   // max
            );
        }

        /// <summary>
        /// 임포스터 렌더링에 사용할 뷰 매트릭스들을 계산한다.
        /// 설정된 수평 및 수직 각도에 따라 다양한 시점에서의 뷰 매트릭스를 생성한다.
        /// </summary>
        /// <param name="bounds">모델의 바운딩 박스</param>
        private void CalculateViewMatrices(ImpostorSettings settings, AABB bounds)
        {
            _viewDataList.Clear();

            // AABB의 중심점과 반지름 계산
            Vertex3f center = bounds.Center;
            float radius = bounds.SphereRadius;
            
            // 각 수평/수직 각도에 대해 뷰 매트릭스 생성
            for (int h = 0; h < settings.HorizontalAngles; h++)
            {
                // 수평 각도 계산 (360도를 HorizontalAngles 만큼 분할)
                float horizontalAngle = (h * 360f) / settings.HorizontalAngles;

                for (int v = 0; v < settings.VerticalAngles; v++)
                {
                    // 수직 각도 계산 (VerticalAngleMin에서 VerticalAngleMax까지 보간)
                    float verticalAngle = MathF.Lerp(
                        settings.VerticalAngleMin,
                        settings.VerticalAngleMax,
                        v / (float)(settings.VerticalAngles - 1)
                    );

                    ViewData viewData = new ViewData
                    {
                        HorizontalAngle = horizontalAngle,
                        VerticalAngle = verticalAngle
                    };

                    // 구면 좌표계를 사용하여 카메라 위치 계산
                    float theta = horizontalAngle.ToRadian(); // 방위각 (xY 평면에서의 각도)
                    float phi = (90 - verticalAngle).ToRadian(); // 극각 (Z축으로부터의 각도)
                    float r = radius * 4.0f; // 카메라 거리

                    // 구면 좌표계를 직교 좌표계로 변환
                    Vertex3f cameraPosition = new Vertex3f(
                        center.x + r * MathF.Sin(phi) * MathF.Cos(theta),
                        center.y + r * MathF.Sin(phi) * MathF.Sin(theta),
                        center.z + r * MathF.Cos(phi)
                    );

                    // 뷰 매트릭스 생성 (카메라가 항상 중심을 바라보도록)
                    viewData.ViewMatrix = Matrix4x4f.LookAt(
                        cameraPosition,   // 카메라 위치
                        center,          // 바라보는 지점 (중심)
                        Vertex3f.UnitZ   // 상향 벡터
                    );

                    // 아틀라스 내 UV 오프셋 계산
                    viewData.AtlasOffset = new Vertex2f(
                        h * settings.IndividualSize / (float)settings.AtlasSize,
                        v * settings.IndividualSize / (float)settings.AtlasSize
                    );

                    _viewDataList.Add(viewData);
                }
            }
        }

        private void RenderEntityAtlas(ImpostorSettings settings, UnlitShader shader, List<Entity> entities,
            AABB chunkBounds, Camera camera)
        {
            // 직교 투영 행렬 설정 - 모델의 실제 크기 유지
            Vertex3f size = chunkBounds.Size;
            float orthoSize = chunkBounds.SphereRadius;

            Matrix4x4f proj = Matrix4x4f.Ortho(
                -orthoSize, orthoSize,
                -orthoSize, orthoSize,
                0.1f, orthoSize * 5.0f
            );

            // 렌더 타겟 설정
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasRenderTarget.FrameBuffer);
            Gl.ClearColor(0, 0, 0, 0);  // 투명으로 초기화
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.Enable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);
            shader.Bind();

            // 각 뷰포인트에서 모델 렌더링
            foreach (ViewData viewData in _viewDataList)
            {
                // 아틀라스 내 해당 뷰의 렌더링 영역 설정
                Gl.Viewport(
                    (int)(settings.AtlasSize * viewData.AtlasOffset.x),
                    (int)(settings.AtlasSize * viewData.AtlasOffset.y),
                    settings.IndividualSize,
                    settings.IndividualSize
                );

                Matrix4x4f vp = proj * viewData.ViewMatrix;

                foreach (Entity entity in entities)
                {
                    if (entity.IsDrawOneSide == false)
                    {
                        Gl.Disable(EnableCap.CullFace);
                    }
                    else
                    {
                        Gl.Enable(EnableCap.CullFace);
                        Gl.CullFace(CullFaceMode.Back);
                    }

                    shader.LoadUniform(UnlitShader.UNIFORM_NAME.mvp, vp * entity.ModelMatrix);

                    // 모델을 그린다.
                    foreach (BaseModel3d rawModel in entity.Models.ToArray())
                    {
                        if (entity.IsTextured)
                        {
                            TexturedModel modelTextured = rawModel as TexturedModel;
                            if (modelTextured.Texture != null)
                            {
                                if (modelTextured.Texture.TextureType.HasFlag(Texture.TextureMapType.Diffuse))
                                {
                                    shader.LoadTexture(UnlitShader.UNIFORM_NAME.modelTexture, TextureUnit.Texture0, modelTextured.Texture.DiffuseMapID);
                                }
                                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                                Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                            }
                        }
                        Gl.BindVertexArray(rawModel.VAO);
                        Gl.EnableVertexAttribArray(0);
                        Gl.EnableVertexAttribArray(1);
                        Gl.DrawArrays(PrimitiveType.Triangles, 0, rawModel.VertexCount);
                        Gl.DisableVertexAttribArray(1);
                        Gl.DisableVertexAttribArray(0);
                        Gl.BindVertexArray(0);
                    }
                }
            }
            shader.Unbind();
        }

        /// <summary>
        /// 실제 아틀라스 렌더링을 수행한다.
        /// 각 뷰포인트에서 모델을 렌더링하여 아틀라스 텍스처를 생성한다.
        /// </summary>
        /// <param name="shader">렌더링에 사용할 쉐이더</param>
        /// <param name="texturedModels">렌더링할 3D 모델들의 배열</param>
        /// <param name="bounds">모델의 바운딩 박스</param>
        private void RenderAtlas(ImpostorSettings settings, UnlitShader shader, TexturedModel[] texturedModels, AABB bounds)
        {
            // 직교 투영 행렬 설정 - 모델의 실제 크기 유지
            Vertex3f size = bounds.Size;
            float orthoSize = bounds.SphereRadius;

            Matrix4x4f proj = Matrix4x4f.Ortho(
                -orthoSize, orthoSize,
                -orthoSize, orthoSize,
                0.1f, orthoSize * 5.0f
            );

            // 렌더 타겟 설정
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasRenderTarget.FrameBuffer);
            Gl.ClearColor(0, 0, 0, 0);  // 투명으로 초기화
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            Gl.Enable(EnableCap.DepthTest);

            // 각 뷰포인트에서 모델 렌더링
            foreach (ViewData viewData in _viewDataList)
            {
                // 아틀라스 내 해당 뷰의 렌더링 영역 설정
                Gl.Viewport(
                    (int)(settings.AtlasSize * viewData.AtlasOffset.x),
                    (int)(settings.AtlasSize * viewData.AtlasOffset.y),
                    settings.IndividualSize,
                    settings.IndividualSize
                );

                Gl.Disable(EnableCap.Blend);

                shader.Bind();

                // 월드뷰투영 행렬 설정
                Matrix4x4f mvp = proj * viewData.ViewMatrix;
                shader.LoadUniform(UnlitShader.UNIFORM_NAME.mvp, mvp);

                // 모델을 그린다.
                foreach (RawModel3d rawModel in texturedModels)
                {
                    if (rawModel is TexturedModel)
                    {
                        TexturedModel modelTextured = rawModel as TexturedModel;
                        if (modelTextured.Texture != null)
                        {
                            if (modelTextured.Texture.TextureType.HasFlag(Texture.TextureMapType.Diffuse))
                            {
                                shader.LoadTexture(UnlitShader.UNIFORM_NAME.modelTexture, TextureUnit.Texture0, modelTextured.Texture.DiffuseMapID);
                            }
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, Gl.REPEAT);
                            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, Gl.REPEAT);
                        }
                    }

                    Gl.BindVertexArray(rawModel.VAO);
                    Gl.EnableVertexAttribArray(0);
                    Gl.EnableVertexAttribArray(1);
                    Gl.DrawArrays(PrimitiveType.Triangles, 0, rawModel.VertexCount);
                    Gl.DisableVertexAttribArray(1);
                    Gl.DisableVertexAttribArray(0);
                    Gl.BindVertexArray(0);
                }

                shader.Unbind();
            }
        }

        /// <summary>
        /// 생성된 임포스터 아틀라스를 Bitmap 형태로 반환한다.
        /// 옵션에 따라 각 뷰의 경계에 테두리를 그릴 수 있다.
        /// </summary>
        /// <param name="drawBorders">테두리 그리기 여부</param>
        /// <returns>임포스터 아틀라스 Bitmap</returns>
        public Bitmap GetImpostorTexture(ImpostorSettings settings, bool drawBorders = false)
        {
            // 픽셀 데이터를 저장할 포인터
            IntPtr pixelsPtr = IntPtr.Zero;

            try
            {
                // 텍스처 메모리 할당
                int size = settings.AtlasSize * settings.AtlasSize * 4;
                pixelsPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

                // 현재 바인딩된 프레임버퍼 저장
                Gl.GetInteger(GetPName.ReadFramebufferBinding, out uint previousFramebuffer);

                // 렌더 타겟 프레임버퍼 바인딩
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasRenderTarget.FrameBuffer);

                // 픽셀 데이터 읽기
                Gl.ReadPixels(
                    0, 0,                          // x, y 좌표
                    settings.AtlasSize,           // 너비
                    settings.AtlasSize,           // 높이
                    PixelFormat.Rgba,             // 픽셀 포맷
                    PixelType.UnsignedByte,       // 데이터 타입
                    pixelsPtr                     // 저장할 메모리 위치
                );

                // 이전 프레임버퍼로 복구
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer);

                // 픽셀 데이터를 관리되는 배열로 복사
                byte[] pixels = new byte[size];
                System.Runtime.InteropServices.Marshal.Copy(pixelsPtr, pixels, 0, size);

                // Bitmap 생성
                Bitmap bitmap = new Bitmap((int)settings.AtlasSize, settings.AtlasSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                // Bitmap 데이터를 직접 조작하기 위해 락
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

                        for (int y = 0; y < settings.AtlasSize; y++)
                        {
                            for (int x = 0; x < settings.AtlasSize; x++)
                            {
                                // OpenGL 데이터는 아래에서 위로 저장되어 있으므로 y좌표를 뒤집어서 읽음
                                int srcIndex = (((settings.AtlasSize - 1 - y) * settings.AtlasSize) + x) * 4;
                                int dstIndex = (y * stride) + (x * 4);

                                // RGBA를 BGRA로 변환 (GDI+의 Format32bppArgb는 BGRA 형식임)
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
                    // 비트맵 언락
                    bitmap.UnlockBits(bitmapData);
                }

                // 현재 시각 표시
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    // 현재 시각 문자열 생성
                    string timeText = DateTime.Now.ToString("HH:mm:ss");

                    // 폰트 설정
                    using (Font font = new Font("Arial", 20, FontStyle.Regular))
                    using (Brush brush = new SolidBrush(Color.Red))
                    {
                        // 문자열 중앙 정렬을 위한 포맷 설정
                        StringFormat stringFormat = new StringFormat();
                        stringFormat.Alignment = StringAlignment.Center;
                        stringFormat.LineAlignment = StringAlignment.Far;

                        // 하단 중앙에 텍스트 그리기
                        RectangleF textRect = new RectangleF(0, 0, bitmap.Width, bitmap.Height - 10);
                        g.DrawString(timeText, font, brush, textRect, stringFormat);
                    }
                }

                // 테두리 그리기
                if (drawBorders)
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        // 합성 모드를 SourceOver로 설정하여 기존 내용 위에 그리기
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;

                        using (Pen pen = new Pen(Color.FromArgb(255, Color.Red))) // 완전 불투명한 빨간색
                        {
                            // 선 품질 설정
                            pen.Width = 2;
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                            // 전체 영역을 IndividualSize로 나누어 모든 그리드에 테두리 그리기
                            int horizontalCells = settings.AtlasSize / settings.IndividualSize;
                            int verticalCells = settings.AtlasSize / settings.IndividualSize;

                            for (int v = 0; v < verticalCells; v++)
                            {
                                for (int h = 0; h < horizontalCells; h++)
                                {
                                    int x = h * settings.IndividualSize;
                                    int y = v * settings.IndividualSize;

                                    // 테두리 그리기 (1픽셀 안쪽으로)
                                    g.DrawRectangle(pen,
                                        x + 1, y + 1,
                                        settings.IndividualSize - 3,  // 양쪽 1픽셀씩 줄임
                                        settings.IndividualSize - 3); // 양쪽 1픽셀씩 줄임
                                }
                            }
                        }
                    }
                }

                return bitmap;
            }
            finally
            {
                // 할당된 메모리 해제
                if (pixelsPtr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(pixelsPtr);
                }
            }
        }

        public Bitmap GetImpostorDepthTexture(ImpostorSettings settings, bool drawBorders = false)
        {
            // 깊이값 데이터를 저장할 포인터
            IntPtr depthPtr = IntPtr.Zero;

            try
            {
                // 깊이 버퍼 메모리 할당 (float 타입)
                int size = settings.AtlasSize * settings.AtlasSize * sizeof(float);
                depthPtr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);

                // 현재 바인딩된 프레임버퍼 저장
                Gl.GetInteger(GetPName.ReadFramebufferBinding, out uint previousFramebuffer);

                // 렌더 타겟 프레임버퍼 바인딩
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasRenderTarget.FrameBuffer);

                // 깊이 데이터 읽기
                Gl.ReadPixels(
                    0, 0,                          // x, y 좌표
                    settings.AtlasSize,            // 너비
                    settings.AtlasSize,            // 높이
                    PixelFormat.DepthComponent,    // 깊이 컴포넌트
                    PixelType.Float,               // float 타입
                    depthPtr                       // 저장할 메모리 위치
                );

                // 이전 프레임버퍼로 복구
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, previousFramebuffer);

                // float 데이터를 관리되는 배열로 복사
                float[] depthValues = new float[settings.AtlasSize * settings.AtlasSize];
                System.Runtime.InteropServices.Marshal.Copy(depthPtr, depthValues, 0, depthValues.Length);

                // Bitmap 생성
                Bitmap bitmap = new Bitmap((int)settings.AtlasSize, settings.AtlasSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                // Bitmap 데이터를 직접 조작하기 위해 락
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

                        for (int y = 0; y < settings.AtlasSize; y++)
                        {
                            for (int x = 0; x < settings.AtlasSize; x++)
                            {
                                // OpenGL 데이터는 아래에서 위로 저장되어 있으므로 y좌표를 뒤집어서 읽음
                                int srcIndex = ((settings.AtlasSize - 1 - y) * settings.AtlasSize) + x;
                                int dstIndex = (y * stride) + (x * 4);

                                // 깊이값을 0-255 범위로 변환
                                byte depthByte = (byte)(depthValues[srcIndex] * 255.0f);

                                // 그레이스케일로 변환 (R=G=B)
                                bitmapPtr[dstIndex + 0] = depthByte; // B
                                bitmapPtr[dstIndex + 1] = depthByte; // G
                                bitmapPtr[dstIndex + 2] = depthByte; // R
                                bitmapPtr[dstIndex + 3] = 255;       // A (불투명)
                            }
                        }
                    }

                    // 테두리 그리기
                    if (drawBorders)
                    {
                        unsafe
                        {
                            byte* bitmapPtr = (byte*)bitmapData.Scan0;
                            int stride = bitmapData.Stride;

                            // 전체 영역을 IndividualSize로 나누어 모든 그리드에 테두리 그리기
                            int horizontalCells = settings.AtlasSize / settings.IndividualSize;
                            int verticalCells = settings.AtlasSize / settings.IndividualSize;

                            for (int v = 0; v < verticalCells; v++)
                            {
                                for (int h = 0; h < horizontalCells; h++)
                                {
                                    int startx = h * settings.IndividualSize;
                                    int startY = v * settings.IndividualSize;
                                    int endx = startx + settings.IndividualSize - 1;
                                    int endY = startY + settings.IndividualSize - 1;

                                    // 테두리 픽셀 그리기
                                    for (int x = startx; x <= endx; x++)
                                    {
                                        // 상단과 하단 테두리
                                        for (int offset = 0; offset < 2; offset++)
                                        {
                                            int y1 = startY + offset;
                                            int y2 = endY - offset;

                                            int idx1 = (y1 * stride) + (x * 4);
                                            int idx2 = (y2 * stride) + (x * 4);

                                            // 빨간색 테두리 설정
                                            bitmapPtr[idx1 + 0] = 0;    // B
                                            bitmapPtr[idx1 + 1] = 0;    // G
                                            bitmapPtr[idx1 + 2] = 255;  // R
                                            bitmapPtr[idx1 + 3] = 255;  // A

                                            bitmapPtr[idx2 + 0] = 0;    // B
                                            bitmapPtr[idx2 + 1] = 0;    // G
                                            bitmapPtr[idx2 + 2] = 255;  // R
                                            bitmapPtr[idx2 + 3] = 255;  // A
                                        }
                                    }

                                    // 좌우 테두리
                                    for (int y = startY; y <= endY; y++)
                                    {
                                        // 왼쪽과 오른쪽 테두리
                                        for (int offset = 0; offset < 2; offset++)
                                        {
                                            int x1 = startx + offset;
                                            int x2 = endx - offset;

                                            int idx1 = (y * stride) + (x1 * 4);
                                            int idx2 = (y * stride) + (x2 * 4);

                                            // 빨간색 테두리 설정
                                            bitmapPtr[idx1 + 0] = 0;    // B
                                            bitmapPtr[idx1 + 1] = 0;    // G
                                            bitmapPtr[idx1 + 2] = 255;  // R
                                            bitmapPtr[idx1 + 3] = 255;  // A

                                            bitmapPtr[idx2 + 0] = 0;    // B
                                            bitmapPtr[idx2 + 1] = 0;    // G
                                            bitmapPtr[idx2 + 2] = 255;  // R
                                            bitmapPtr[idx2 + 3] = 255;  // A
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    // 비트맵 언락
                    bitmap.UnlockBits(bitmapData);
                }

                return bitmap;
            }
            finally
            {
                // 할당된 메모리 해제
                if (depthPtr != IntPtr.Zero)
                {
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(depthPtr);
                }
            }
        }

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            _atlasRenderTarget?.Dispose();
        }
    }
}

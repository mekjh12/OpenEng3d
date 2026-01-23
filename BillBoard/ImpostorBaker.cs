using Common;
using Common.Abstractions;
using Model3d;
using Newtonsoft.Json;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace BillBoard
{
    /// <summary>
    /// 임포스터 아틀라스를 베이킹하는 클래스 (MRT 지원)
    /// AABB 중심 기준으로 생성, Transform으로 배치/크기 조정
    /// </summary>
    public class ImpostorBaker : IDisposable
    {
        private RenderTarget2D _atlasRenderTarget;
        private List<ViewData> _viewDataList = new List<ViewData>();
        private UnifiedTexturedModel _model;

        private struct ViewData
        {
            public Matrix4x4f ViewMatrix;
            public int HorizontalIndex;
            public int VerticalIndex;
        }

        /// <summary>
        /// 임포스터 아틀라스 베이킹 (MRT - Albedo, Normal, Depth)
        /// </summary>
        public ImpostorBakeResult BakeAtlas(
            UnifiedTexturedModel model,
            ImpostorSettings settings,
            ImpostorBakingShader shader,
            string outputPath = null)
        {
            // 1. 렌더 타겟 초기화 (MRT - 3개 컬러 어태치먼트)
            InitializeRenderTarget(settings);

            // 2. 모델 렌더러 생성
            _model = model;

            // 3. AABB 기반 바운딩 계산 (중심 기준)
            BoundingData bounds = CalculateBounds(model.AABB, settings.PaddingFactor);

            // 4. 뷰 매트릭스 계산
            CalculateViewMatrices(settings, bounds);

            // 5. 아틀라스 렌더링
            RenderAtlas(settings, shader, bounds);

            // 6. 메타데이터 생성
            ImpostorMetadata metadata = CreateMetadata(settings, bounds);

            // 7. 메타데이터 저장 (옵션)
            if (outputPath != null)
            {
                string basePath = Path.GetDirectoryName(outputPath);
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(outputPath);
                string pngPathAlbedo = Path.Combine(basePath, fileNameWithoutExt + "_albedo.png");
                string pngPathNormal = Path.Combine(basePath, fileNameWithoutExt + "_normal.png");
                string pngPathDepth = Path.Combine(basePath, fileNameWithoutExt + "_depth.png");

                ImposterSaver.GetImpostorTexture(_atlasRenderTarget, settings, false).Save(pngPathAlbedo);
                ImposterSaver.GetImpostorNormalTexture(_atlasRenderTarget, settings, false).Save(pngPathNormal);
                ImposterSaver.GetImpostorDepthTexture(_atlasRenderTarget, settings, false).Save(pngPathDepth);

                SaveMetadata(metadata, outputPath);
            }

            // 8. 결과 반환 (MRT - 3개 텍스처)
            return new ImpostorBakeResult
            {
                AlbedoTextureID = _atlasRenderTarget.GetColorTexture(0),
                NormalTextureID = _atlasRenderTarget.GetColorTexture(1),
                DepthTextureID = _atlasRenderTarget.GetColorTexture(2),
                Metadata = metadata
            };
        }

        /// <summary>
        /// 렌더 타겟 초기화 (MRT - 3개 컬러 어태치먼트)
        /// </summary>
        private void InitializeRenderTarget(ImpostorSettings settings)
        {
            if (_atlasRenderTarget != null)
            {
                _atlasRenderTarget.Dispose();
            }

            // RenderTarget2D 생성 (Albedo, Normal, Depth용)
            _atlasRenderTarget = new RenderTarget2D(
                settings.AtlasSize,
                settings.AtlasSize,
                false, // generateMips
                SurfaceFormat.Color,
                DepthFormat.Depth24,
                3 // ✅ 3개 컬러 어태치먼트 (Albedo, Normal, Depth)
            );

            // 투명 배경으로 클리어
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasRenderTarget.FrameBuffer);
            Gl.ClearColor(0, 0, 0, 0);
            Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }


        /// <summary>
        /// 사용할 바운딩 계산 (AABB 기반, 패딩 적용)
        /// </summary>
        private BoundingData CalculateBounds(AABB3f modelAABB, float paddingFactor)
        {
            float padding = 1.0f + paddingFactor;

            // AABB3f의 프로퍼티 활용
            Vertex3f center = modelAABB.Center;
            Vertex3f size = new Vertex3f(
                modelAABB.Size.x * padding,
                modelAABB.Size.y * padding,
                modelAABB.Size.z * padding
            );

            // Bounding Sphere 반경 (AABB의 Radius 프로퍼티 활용)
            float radius = modelAABB.Radius * padding;

            return new BoundingData
            {
                Center = center,
                Size = size,
                Radius = radius
            };
        }

        /// <summary>
        /// 뷰 매트릭스 계산 (구면 좌표계)
        /// </summary>
        private void CalculateViewMatrices(ImpostorSettings settings, BoundingData bounds)
        {
            _viewDataList.Clear();

            // 카메라 거리 (반경의 4배)
            float cameraDistance = bounds.Radius * 4.0f;
            Vertex3f lookAtPoint = bounds.Center;

            for (int h = 0; h < settings.HorizontalAngles; h++)
            {
                // 수평 각도 (0° ~ 360°, Y축 기준)
                float horizontalAngle = (h / (float)settings.HorizontalAngles) * 360.0f;
                float hRadians = (float)(horizontalAngle * Math.PI / 180.0f);

                for (int v = 0; v < settings.VerticalAngles; v++)
                {
                    // 수직 각도 (min ~ max)
                    float t = v / (float)(settings.VerticalAngles - 1);
                    float verticalAngle = settings.VerticalAngleMin +
                                         t * (settings.VerticalAngleMax - settings.VerticalAngleMin);
                    float vRadians = (float)(verticalAngle * Math.PI / 180.0f);

                    // 구면 좌표계 -> 카르테시안 좌표계
                    float x = (float)(cameraDistance * Math.Cos(vRadians) * Math.Cos(hRadians));
                    float y = (float)(cameraDistance * Math.Cos(vRadians) * Math.Sin(hRadians));
                    float z = (float)(cameraDistance * Math.Sin(vRadians));

                    Vertex3f cameraPosition = new Vertex3f(
                        lookAtPoint.x + x,
                        lookAtPoint.y + y,
                        lookAtPoint.z + z
                    );

                    Matrix4x4f viewMatrix = Matrix4x4f.LookAt(
                        cameraPosition,
                        lookAtPoint,
                        Vertex3f.UnitZ
                    );

                    _viewDataList.Add(new ViewData
                    {
                        ViewMatrix = viewMatrix,
                        HorizontalIndex = h,
                        VerticalIndex = v
                    });
                }
            }
        }

        /// <summary>
        /// 아틀라스 렌더링 (MRT)
        /// </summary>
        private void RenderAtlas(ImpostorSettings settings, ImpostorBakingShader shader, BoundingData bounds)
        {
            // 직교 투영 (대칭)
            //float maxDimension = Math.Max(Math.Max(bounds.Size.x, bounds.Size.y), bounds.Size.z);
            //float orthoSize = maxDimension * 0.5f;  // 절반 사용

            float orthoSize = bounds.Radius;

            // Far plane은 카메라 거리 기준으로!
            float cameraDistance = bounds.Radius * 4.0f;
            float nearPlane = 0.1f;
            float farPlane = cameraDistance + bounds.Radius * 2.0f;  // 여유있게

            Matrix4x4f projMatrix = Matrix4x4f.Ortho(
                -orthoSize, orthoSize,
                -orthoSize, orthoSize,
                nearPlane, farPlane
            );

            // RenderTarget2D 바인딩
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, _atlasRenderTarget.FrameBuffer);

            // 깊이 테스트 활성화
            Gl.Enable(EnableCap.DepthTest);
            Gl.DepthFunc(DepthFunction.Less);

            // 각 뷰별 렌더링
            int frameIndex = 0;
            foreach (var viewData in _viewDataList)
            {
                // 뷰포트 설정 (아틀라스 내 위치)
                int row = viewData.VerticalIndex;
                int col = viewData.HorizontalIndex;
                int x = col * settings.IndividualSize;
                int y = row * settings.IndividualSize;

                Gl.Viewport(x, y, settings.IndividualSize, settings.IndividualSize);

                // MVP, MV 행렬 계산
                Matrix4x4f model = Matrix4x4f.Identity;
                Matrix4x4f mv = viewData.ViewMatrix * model;
                Matrix4x4f mvp = projMatrix * mv;

                // 셰이더 유니폼 설정
                shader.Bind();
                {
                    shader.LoadTextureArray(_model.TextureIDArray);
                    shader.LoadTransforms(mvp, mv, Matrix4x4f.Identity);

                    Gl.BindVertexArray(_model.VaoID);
                    Gl.DrawElements(
                        PrimitiveType.Triangles,
                        _model.IndexCount,
                        DrawElementsType.UnsignedInt,
                        IntPtr.Zero);
                    Gl.BindVertexArray(0);
                }
                shader.Unbind();

                frameIndex++;
            }

            // RenderTarget2D 언바인딩
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            // 뷰포트 복원
            Gl.Viewport(0, 0, settings.AtlasSize, settings.AtlasSize);
        }

        /// <summary>
        /// 메타데이터 생성
        /// </summary>
        private ImpostorMetadata CreateMetadata(ImpostorSettings settings, BoundingData bounds)
        {
            return new ImpostorMetadata
            {
                ModelName = settings.Name,
                GeneratedAt = DateTime.Now,

                // AABB 정보
                AABBCenter = new Vector3f(bounds.Center.x, bounds.Center.y, bounds.Center.z),
                AABBSize = new Vector3f(bounds.Size.x, bounds.Size.y, bounds.Size.z),
                BoundingSphereRadius = bounds.Radius,

                // 아틀라스 정보
                AtlasSize = settings.AtlasSize,
                IndividualSize = settings.IndividualSize,
                HorizontalAngles = settings.HorizontalAngles,
                VerticalAngles = settings.VerticalAngles,
                VerticalAngleMin = settings.VerticalAngleMin,
                VerticalAngleMax = settings.VerticalAngleMax,

                // 렌더링 가이드
                AtlasUVScale = settings.IndividualSize / (float)settings.AtlasSize,
                TotalFrames = settings.HorizontalAngles * settings.VerticalAngles
            };
        }

        /// <summary>
        /// 메타데이터 JSON 저장
        /// </summary>
        private void SaveMetadata(ImpostorMetadata metadata, string outputPath)
        {
            string json = JsonConvert.SerializeObject(metadata, Formatting.Indented);
            File.WriteAllText(outputPath, json);
        }

        public void Dispose()
        {
            _atlasRenderTarget?.Dispose();
        }

        /// <summary>
        /// 바운딩 데이터 (내부 사용)
        /// </summary>
        private struct BoundingData
        {
            public Vertex3f Center;
            public Vertex3f Size;
            public float Radius;
        }
    }

    /// <summary>
    /// 베이킹 결과 (MRT - 3개 텍스처)
    /// </summary>
    public class ImpostorBakeResult
    {
        public uint AlbedoTextureID { get; set; }   // ColorAttachment0
        public uint NormalTextureID { get; set; }   // ColorAttachment1
        public uint DepthTextureID { get; set; }    // ColorAttachment2
        public ImpostorMetadata Metadata { get; set; }
    }
}
using Common.Abstractions;
using OpenGL;
using Shader;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Renderer
{
    public class GrassRenderer
    {        
        private const int MAX_GRASS_INSTANCES = 1_000_000;

        private uint _grassSSBO;                    // 풀 위치 SSBO
        private uint _indirectCommandBuffer;        // DrawIndirect 커맨드
        private uint _dummyVAO;                     // 빈 VAO (데이터 없음!)

        private List<GrassInstanceData> _allGrass;  // CPU 측 데이터
        private int _currentGrassCount;             // 현재 풀 개수

        private GrassShader _shader;
        private uint _grassTextureID;
        private string _projectPath;

        public GrassRenderer(string projectPath)
        {
            // 프로젝트 경로 저장
            _projectPath = projectPath;

            // 셰이더 로드
            _shader = new GrassShader(projectPath);

            // 텍스처 로드
            _grassTextureID = new Texture(projectPath + @"\Res\PT_Grass_02.png").TextureID;

            // SSBO 생성
            CreateGrassSSBO();

            // Indirect Command Buffer 생성
            CreateIndirectCommandBuffer();

            // 더미 VAO 생성 (중요!)
            CreateDummyVAO();

            // 풀 인스턴스 리스트 초기화
            _allGrass = new List<GrassInstanceData>();

        }

        public void Render(Camera camera, Vertex3f sunDirection)
        {
            if (_currentGrassCount == 0) return;

            // 렌더 스테이트 설정
            //Gl.Enable(EnableCap.Blend);
            //Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            //Gl.Enable(EnableCap.DepthTest);
            //Gl.DepthMask(true);
            //Gl.Disable(EnableCap.CullFace);

            // 셰이더 바인딩
            _shader.Bind();

            // Uniform 설정
            _shader.LoadCameraVectors(camera.Right, new Vertex3f(0, 0, 1));
            _shader.LoadGrassSize(1f, 1f);
            _shader.LoadTexture(_grassTextureID);
            _shader.LoadSunDirection(sunDirection); // 지표에서 태양으로 향하는 벡터
            _shader.LoadGrassColors(
                new Vertex3f(0.5f, 0.8f, 0.3f),  // 상단: 밝은 초록
                new Vertex3f(0.2f, 0.4f, 0.1f)   // 하단: 어두운 초록
            );

            // SSBO 바인딩
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _grassSSBO);

            // VAO 바인딩 (더미 VAO!)
            Gl.BindVertexArray(_dummyVAO);

            // Indirect 커맨드 바인딩
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);

            // DrawIndirect 호출
            Gl.DrawArraysIndirect(PrimitiveType.TriangleStrip, IntPtr.Zero);

            // 언바인딩
            _shader.Unbind();

            Gl.BindVertexArray(0);
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, 0);

            // 렌더 스테이트 복원
            Gl.Disable(EnableCap.Blend);
            Gl.Enable(EnableCap.CullFace);
        }

        /// <summary>
        /// 모든 활성 타일에서 풀 수집 및 GPU 업로드
        /// </summary>
        public void UpdateGrassData(GrassTileManager tileManager, Camera camera)
        {
            // 타일이 업데이트되지 않았다면 무시
            if (tileManager.IsTilesUpdated == false) return;

            // 모든 활성 타일에서 풀 수집
            _allGrass.Clear();

            foreach (var tile in tileManager.GetActiveTiles())
            {
                tile.GetGrassInstances(ref _allGrass);
            }

            _currentGrassCount = _allGrass.Count;

            if (_currentGrassCount == 0)
                return;

            // GPU로 업로드
            UploadToGPU();

            // Indirect 커맨드 업데이트
            UpdateIndirectCommand();
        }

        private IntPtr _persistentMappedPtr;  // GPU 메모리에 직접 접근


        private void UploadToGPU()
        {
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _grassSSBO);

            // 데이터 크기 계산
            int sizeInBytes = _currentGrassCount * Marshal.SizeOf<GrassInstanceData>();

            unsafe
            {
                fixed (GrassInstanceData* ptr = _allGrass.ToArray())
                {
                    Gl.BufferSubData(
                        BufferTarget.ShaderStorageBuffer,
                        IntPtr.Zero,
                        (uint)sizeInBytes,
                        (IntPtr)ptr
                    );
                }
            }

            Console.WriteLine($"[Grass] Uploaded {_currentGrassCount} instances " + $"({sizeInBytes / 1024.0:F1} KB)");
        }

        private void UpdateIndirectCommand()
        {
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);

            DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand
            {
                VertexCount = 4,
                InstanceCount = (uint)_currentGrassCount,
                First = 0,
                BaseInstance = 0
            };

            Console.WriteLine($"[Indirect Command] InstanceCount = {cmd.InstanceCount}");

            // ✅ 수정: unsafe로 포인터 전달
            unsafe
            {
                Gl.BufferSubData(
                    BufferTarget.DrawIndirectBuffer,
                    IntPtr.Zero,
                    (uint)Marshal.SizeOf<DrawArraysIndirectCommand>(),
                    new IntPtr(&cmd)  // ← 포인터로 전달!
                );
            }
        }

        private void CreateGrassSSBO()
        {
            // SSBO 생성
            _grassSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _grassSSBO);

            // 최대 크기로 할당
            uint sizeInBytes = (uint)(MAX_GRASS_INSTANCES * Marshal.SizeOf<GrassInstanceData>());

            Gl.BufferData(
                BufferTarget.ShaderStorageBuffer,
                sizeInBytes,
                IntPtr.Zero,
                BufferUsage.DynamicDraw  // 동적 업데이트 가능
            );

            // 바인딩 포인트 0에 연결
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, 0, _grassSSBO);

            Console.WriteLine($"[Grass SSBO] Created: {sizeInBytes / 1024.0 / 1024.0:F2} MB");
        }

        private void CreateIndirectCommandBuffer()
        {
            _indirectCommandBuffer = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, _indirectCommandBuffer);

            // DrawArraysIndirectCommand 초기화
            DrawArraysIndirectCommand cmd = new DrawArraysIndirectCommand
            {
                VertexCount = 4,        // 쿼드 = 4 버텍스 (Triangle Strip)
                InstanceCount = 0,      // 나중에 업데이트
                First = 0,
                BaseInstance = 0
            };

            Gl.BufferData(
                BufferTarget.DrawIndirectBuffer,
                (uint)Marshal.SizeOf<DrawArraysIndirectCommand>(),
                cmd,
                BufferUsage.DynamicDraw
            );

            Gl.BindBuffer(BufferTarget.DrawIndirectBuffer, 0);
        }

        private void CreateDummyVAO()
        {
            _dummyVAO = Gl.GenVertexArray();
            Gl.BindVertexArray(_dummyVAO);

            // 아무 버퍼도 바인딩 안 함!
            // Vertex Shader가 gl_VertexID와 gl_InstanceID만으로 작동

            Gl.BindVertexArray(0);
        }
    }

    /// <summary>
    /// 풀 인스턴스 데이터 구조체
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DrawArraysIndirectCommand
    {
        public uint VertexCount;
        public uint InstanceCount;
        public uint First;
        public uint BaseInstance;
    }

}

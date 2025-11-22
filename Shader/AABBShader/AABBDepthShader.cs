using Common;
using Common.Abstractions;
using OpenGL;
using System;
using System.Runtime.InteropServices;

namespace Shader
{
    /// <summary>
    /// AABB 배열을 받아 Hierarchical Z-Buffer에 깊이를 렌더링하는 셰이더
    /// GPU 인스턴싱을 사용하여 여러 AABB를 한 번에 처리합니다.
    /// </summary>
    public class AABBDepthShader : ShaderProgramBase
    {
        const string VERTEX_FILE = @"\Shader\AABBShader\aabb_depth.vert";
        const string GEOMETRY_FILE = @"\Shader\AABBShader\aabb_depth.gem.glsl";
        const string FRAGMENT_FILE = @"\Shader\AABBShader\aabb_depth.frag";

        // 유니폼 위치 캐싱
        private int loc_view;
        private int loc_proj;

        // SSBO 바인딩 포인트
        private const int AABB_BUFFER_BINDING = 0;

        // GPU 버퍼
        private uint _aabbSSBO;
        private uint _dummyVAO;
        private uint _aabbDepthVAO = 0;

        // GPU 데이터 변환용 임시 버퍼 (재사용)
        private AABBGpuData[] _gpuDataBuffer;
        private int _gpuDataBufferCapacity = 0;

        public AABBDepthShader(string projectPath) : base()
        {
            _name = this.GetType().Name;
            VertFileName = projectPath + VERTEX_FILE;
            GeomFileName = projectPath + GEOMETRY_FILE;
            FragFileName = projectPath + FRAGMENT_FILE;

            InitCompileShader();
            InitBuffers();
            Create();
        }

        /// <summary>
        /// 렌더링에 필요한 VAO와 SSBO를 생성합니다.
        /// </summary>
        protected void Create()
        {
            // VAO 생성 (OpenGL 코어 프로파일에서는 VAO가 필수)
            if (_aabbDepthVAO == 0)
            {
                _aabbDepthVAO = Gl.GenVertexArray();
                Gl.BindVertexArray(_aabbDepthVAO);
                Gl.BindVertexArray(0);
            }

            // SSBO 생성
            if (_aabbSSBO == 0)
            {
                _aabbSSBO = Gl.GenBuffer();
            }
        }

        protected override void GetAllUniformLocations()
        {
            loc_view = GetUniformLocation("view");
            loc_proj = GetUniformLocation("proj");
        }

        protected override void BindAttributes()
        {
            // SSBO를 사용하므로 애트리뷰트 바인딩 불필요
        }

        /// <summary>
        /// GPU 버퍼를 초기화합니다. (Dummy VAO)
        /// </summary>
        private void InitBuffers()
        {
            // Dummy VAO 생성 (레거시, 필요시 사용)
            _dummyVAO = Gl.GenVertexArray();
        }

        /// <summary>
        /// AABB 데이터를 GPU SSBO로 업로드합니다.
        /// AABB3f를 GPU 호환 형식(AABBGpuData)으로 변환하여 전송합니다.
        /// </summary>
        private void UploadAABBsToGPU(in AABB3f[] aabbs, int count)
        {
            // GPU 데이터 버퍼 크기 확인 및 필요시 확장
            if (_gpuDataBuffer == null || _gpuDataBufferCapacity < count)
            {
                _gpuDataBufferCapacity = count;
                _gpuDataBuffer = new AABBGpuData[count];
            }

            // AABB3f를 GPU 호환 형식으로 변환
            for (int i = 0; i < count; i++)
            {
                _gpuDataBuffer[i] = new AABBGpuData(aabbs[i]);
            }

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _aabbSSBO);

            unsafe
            {
                fixed (AABBGpuData* ptr = _gpuDataBuffer)
                {
                    int sizeInBytes = Marshal.SizeOf<AABBGpuData>() * count;
                    Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                        (uint)sizeInBytes,
                        new IntPtr(ptr),
                        BufferUsage.DynamicDraw);
                }
            }

            // SSBO를 바인딩 포인트에 연결
            Gl.BindBufferBase(BufferTarget.ShaderStorageBuffer, AABB_BUFFER_BINDING, _aabbSSBO);
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        /// <summary>
        /// 뷰 행렬을 설정합니다.
        /// </summary>
        public void LoadViewMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_view, matrix);
        }

        /// <summary>
        /// 투영 행렬을 설정합니다.
        /// </summary>
        public void LoadProjectionMatrix(in Matrix4x4f matrix)
        {
            LoadUniformMatrix4(loc_proj, matrix);
        }

        /// <summary>
        /// 렌더링 상태를 설정합니다.
        /// </summary>
        private void SetupRenderState()
        {
            Gl.Enable(EnableCap.DepthTest);
            //Gl.DepthFunc(DepthFunction.Less);
            //Gl.DepthMask(true);
            Gl.Disable(EnableCap.CullFace);
            Gl.ColorMask(false, false, false, false);  // 깊이만 렌더링
        }

        /// <summary>
        /// 렌더링 상태를 복원합니다.
        /// </summary>
        private void RestoreRenderState()
        {
            Gl.ColorMask(true, true, true, true);
            Gl.Enable(EnableCap.CullFace);
        }

        /// <summary>
        /// AABB 배열을 Hi-Z 버퍼에 깊이 렌더링합니다.
        /// </summary>
        /// <param name="aabbs">렌더링할 AABB 배열</param>
        /// <param name="count">렌더링할 개수</param>
        /// <param name="camera">카메라</param>
        public void RenderAABBDepth(in AABB3f[] aabbs, int count, Camera camera)
        {
            if (count <= 0 || aabbs == null) return;

            // AABB 데이터 업로드
            UploadAABBsToGPU(aabbs, count);

            // 렌더링 상태 설정
            SetupRenderState();

            // 셰이더 시작 및 유니폼 설정
            Bind();
            LoadViewMatrix(camera.ViewMatrix);
            LoadProjectionMatrix(camera.ProjectiveMatrix);

            // 렌더링 - Geometry Shader가 각 점을 AABB로 확장
            Gl.BindVertexArray(_aabbDepthVAO);
            Gl.DrawArrays(PrimitiveType.Points, 0, count);
            Gl.BindVertexArray(0);

            Unbind();

            // 상태 복원
            RestoreRenderState();
        }


    }

    /// <summary>
    /// GPU SSBO와 호환되는 AABB 구조체 (std430 레이아웃)
    /// GLSL의 vec3은 16바이트 정렬되므로 패딩 추가
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct AABBGpuData
    {
        public Vertex3f Min;      // 12 bytes
        private float _pad1;      // 4 bytes (패딩)
        public Vertex3f Max;      // 12 bytes
        private float _pad2;      // 4 bytes (패딩)
        // 총 32 bytes

        public AABBGpuData(in AABB3f aabb)
        {
            Min = aabb.Min;
            Max = aabb.Max;
            _pad1 = 0;
            _pad2 = 0;
        }
    }
}
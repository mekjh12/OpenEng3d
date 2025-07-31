using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// UBO(Uniform Buffer Object)를 사용하여 본 행렬을 한 번에 GPU로 전송하는 클래스
    /// 기존의 개별 유니폼 호출을 대체하여 성능을 크게 향상시킵니다.
    /// </summary>
    public class BoneMatrixUBO : IDisposable
    {
        private const int MAX_BONES = 128;
        private const int BINDING_POINT = 0; // 셰이더의 binding point와 일치해야 함

        private uint _uboHandle;
        private bool _isInitialized = false;
        private bool _disposed = false;

        /// <summary>
        /// UBO를 초기화합니다.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            unsafe
            {
                // UBO 생성
                _uboHandle = Gl.GenBuffer();
                Gl.BindBuffer(BufferTarget.UniformBuffer, _uboHandle);

                // 본 행렬 저장을 위한 메모리 할당
                // Matrix4x4f = 16 floats = 64 bytes
                int bufferSize = MAX_BONES * 16 * sizeof(float);
                Gl.BufferData(BufferTarget.UniformBuffer, (uint)bufferSize, IntPtr.Zero, BufferUsage.DynamicDraw);

                // 바인딩 포인트에 UBO 연결
                Gl.BindBufferBase(BufferTarget.UniformBuffer, BINDING_POINT, _uboHandle);

                // 바인딩 해제
                Gl.BindBuffer(BufferTarget.UniformBuffer, 0);
            }

            _isInitialized = true;
            Console.WriteLine($"BoneMatrixUBO initialized with handle: {_uboHandle}");
        }

        /// <summary>
        /// 본 행렬 배열을 UBO로 한 번에 업로드합니다.
        /// 기존의 개별 유니폼 호출을 대체합니다.
        /// </summary>
        /// <param name="boneMatrices">업로드할 본 행렬 배열</param>
        public unsafe void UploadBoneMatrices(Matrix4x4f[] boneMatrices)
        {
            if (!_isInitialized)
            {
                Console.WriteLine("Warning: BoneMatrixUBO not initialized. Call Initialize() first.");
                return;
            }

            if (boneMatrices == null || boneMatrices.Length == 0)
            {
                Console.WriteLine("Warning: Empty bone matrices array");
                return;
            }

            int matrixCount = Math.Min(boneMatrices.Length, MAX_BONES);

            // UBO 바인딩
            Gl.BindBuffer(BufferTarget.UniformBuffer, _uboHandle);

            // 전체 본 행렬을 한 번에 업로드
            fixed (Matrix4x4f* matrixPtr = boneMatrices)
            {
                int dataSize = matrixCount * sizeof(Matrix4x4f);
                Gl.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, (uint)dataSize, new IntPtr(matrixPtr));
            }

            // 바인딩 해제
            Gl.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        /// <summary>
        /// 셰이더 프로그램에 UBO를 연결합니다.
        /// </summary>
        /// <param name="shaderProgram">셰이더 프로그램 ID</param>
        /// <param name="blockName">셰이더에서 정의한 uniform block 이름 (예: "BoneMatrices")</param>
        public void BindToShader(uint shaderProgram, string blockName = "BoneMatrices")
        {
            if (!_isInitialized) return;

            // 셰이더에서 uniform block 인덱스 가져오기
            uint blockIndex = Gl.GetUniformBlockIndex(shaderProgram, blockName);

            if (blockIndex == Gl.INVALID_INDEX)
            {
                Console.WriteLine($"Warning: Uniform block '{blockName}' not found in shader program {shaderProgram}");
                return;
            }

            // uniform block을 바인딩 포인트에 연결
            Gl.UniformBlockBinding(shaderProgram, blockIndex, BINDING_POINT);

            Console.WriteLine($"UBO bound to shader program {shaderProgram}, block: {blockName}, binding point: {BINDING_POINT}");
        }

        public void Dispose()
        {
            if (!_disposed && _isInitialized)
            {
                Gl.DeleteBuffers(_uboHandle);
                _uboHandle = 0;
                _isInitialized = false;
                _disposed = true;
                Console.WriteLine("BoneMatrixUBO disposed");
            }
        }
    }
}

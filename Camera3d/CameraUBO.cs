using OpenGL;
using Common;
using System;

namespace Camera3d
{
    /// <summary>
    /// 카메라 관련 행렬을 관리하는 UBO
    /// Binding Point: 0
    /// 포함: view, proj, vp, cameraPos
    /// </summary>
    public class CameraUBO : IDisposable
    {
        private const int BINDING_POINT = 0;
        private uint _uboHandle;

        // std140 레이아웃: 각 mat4는 64바이트 (16 * 4)
        private const int MATRIX_SIZE = 64;
        private const int VEC3_SIZE = 16;  // ✅ std140에서 vec3는 16바이트로 정렬
        private const int BUFFER_SIZE = MATRIX_SIZE * 3 + VEC3_SIZE; // view, proj, vp, cameraPos

        // 오프셋
        private const int OFFSET_VIEW = 0;
        private const int OFFSET_PROJ = 64;
        private const int OFFSET_VP = 128;
        private const int OFFSET_CAMERA_POS = 192;

        public CameraUBO()
        {
            // UBO 생성
            _uboHandle = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.UniformBuffer, _uboHandle);
            Gl.BufferData(BufferTarget.UniformBuffer, BUFFER_SIZE, IntPtr.Zero, BufferUsage.DynamicDraw);

            // Binding Point에 연결
            Gl.BindBufferBase(BufferTarget.UniformBuffer, BINDING_POINT, _uboHandle);
            Gl.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        /// <summary>
        /// 카메라 행렬들과 위치를 한 번에 업데이트 (프레임당 1회 호출)
        /// </summary>
        public void UpdateViewProjMatrices(in Matrix4x4f view, in Matrix4x4f proj, in Vertex3f cameraPos)
        {
            Gl.BindBuffer(BufferTarget.UniformBuffer, _uboHandle);

            // vp 계산
            Matrix4x4f vp = proj * view;

            // 각 행렬을 UBO에 업데이트
            unsafe
            {
                Gl.BufferSubData(BufferTarget.UniformBuffer, (IntPtr)OFFSET_VIEW, MATRIX_SIZE, view);
                Gl.BufferSubData(BufferTarget.UniformBuffer, (IntPtr)OFFSET_PROJ, MATRIX_SIZE, proj);
                Gl.BufferSubData(BufferTarget.UniformBuffer, (IntPtr)OFFSET_VP, MATRIX_SIZE, vp);
                // ✅ 카메라 위치 업데이트 (vec3는 12바이트지만 16바이트로 정렬)
                Gl.BufferSubData(BufferTarget.UniformBuffer, (IntPtr)OFFSET_CAMERA_POS, 12, cameraPos);
            }

            Gl.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        public void Dispose()
        {
            if (_uboHandle != 0)
            {
                Gl.DeleteBuffers(_uboHandle);
                _uboHandle = 0;
            }
        }
    }
}
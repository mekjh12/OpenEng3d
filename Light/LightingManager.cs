using OpenGL;
using System;
using System.Runtime.InteropServices;

namespace Light
{
    /// <summary>
    /// 광원을 관리하는 클래스(UBO 사용)
    /// </summary>
    public class LightingManager
    {
        private uint _uboHandle;
        private const int BINDING_POINT = 1;  // 모든 셰이더가 binding = 1 사용
        private bool _isDirty = true;

        public SceneLighting Lighting { get; set; }

        public LightingManager()
        {
            Lighting = new SceneLighting();
            CreateUBO();
        }

        private void CreateUBO()
        {
            // UBO 생성
            _uboHandle = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.UniformBuffer, _uboHandle);

            // 버퍼 크기 할당 (48 bytes)
            int size = Marshal.SizeOf<LightingUBO>();
            Gl.BufferData(
                BufferTarget.UniformBuffer,
                (uint)size,
                IntPtr.Zero,
                BufferUsage.DynamicDraw
            );

            // Binding Point 연결
            Gl.BindBufferBase(BufferTarget.UniformBuffer, BINDING_POINT, _uboHandle);

            Gl.BindBuffer(BufferTarget.UniformBuffer, 0);

            Console.WriteLine($"[LightingManager] UBO Created: Handle={_uboHandle}, Size={size} bytes");
        }

        public void SetDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// 새로운 정보가 업데이트 되었을 때 UBO를 갱신합니다.
        /// </summary>
        public void Update()
        {
            if (!_isDirty) return;

            LightingUBO data = Lighting.ToUBO();

            Gl.BindBuffer(BufferTarget.UniformBuffer, _uboHandle);

            unsafe
            {
                // ✅ 스택에 할당
                LightingUBO* ptr = stackalloc LightingUBO[1];
                ptr[0] = data;

                Gl.BufferSubData(
                    BufferTarget.UniformBuffer,
                    IntPtr.Zero,
                    (uint)Marshal.SizeOf<LightingUBO>(),
                    new IntPtr(ptr)
                );
            }

            Gl.BindBuffer(BufferTarget.UniformBuffer, 0);

            _isDirty = false;
        }

        public void Cleanup()
        {
            if (_uboHandle != 0)
            {
                Gl.DeleteBuffers(_uboHandle);
                _uboHandle = 0;
            }
        }
    }
}

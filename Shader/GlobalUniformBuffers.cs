using OpenGL;
using System;
using Buffer = System.Buffer;

namespace Shader
{
    /// <summary>
    /// 전역 Uniform Buffer Objects 관리 클래스
    /// </summary>
    /// <remarks>
    /// 새 UBO 버퍼 추가 단계:
    /// 1. 상수 추가: 바인딩 포인트 정의 (NEW_UBO_BINDING_POINT)
    /// 2. 속성 추가: 버퍼 ID 속성 (NewUBO)
    /// 3. Initialize: 버퍼 생성 및 초기화 코드
    /// 4. Update 메서드: 데이터 업데이트 함수 구현
    /// 5. BindUBOsToShader: 셰이더 바인딩 코드 추가
    /// 6. Cleanup: 리소스 해제 코드 추가
    /// 7. GLSL: layout(std140, binding=N) uniform NewUniforms {}
    /// </remarks>
    public static class GlobalUniformBuffers
    {
        /// <summary>
        /// UBO 블록 이름 정의
        /// </summary>
        public enum UBOBlockName
        {
            HalfFogUniforms,
            CameraUniforms,
            DistanceFogUniforms
        }

        // 자료형 크기 및 정렬 관련 상수
        private const int SIZE_VEC4 = 16;  // vec4, mat4의 열 (4 floats, 16바이트 정렬)
        private const int SIZE_VEC3 = 16;  // vec3 (std140에서 vec4 공간 차지, 16바이트 정렬)
        private const int SIZE_FLOAT = 4;  // float (4바이트)
        private const int SIZE_INT = 4;    // int, bool (4바이트)
        private const int SIZE_MAT4 = 64;  // mat4 (4 * vec4, 64바이트)

        // 안개 관련 UBO
        private static uint _halfPlaneFogUBO;
        public const int HALFPLANE_FOG_UBO_BINDING_POINT = 0;

        // 카메라 관련 UBO
        public static uint _cameraUBO;
        public const int CAMERA_UBO_BINDING_POINT = 1;

        // 거리 기반 안개 UBO
        private static uint _distanceFogUBO;
        public const int DISTANCE_FOG_UBO_BINDING_POINT = 2; // 새 바인딩 포인트는 2


        /// <summary>
        /// 시스템 초기화 시 호출하여 UBO들을 초기화합니다.
        /// </summary>
        public static void Initialize()
        {
            Console.WriteLine("-----[UBO 정보]------");

            // 최대 UBO 크기 출력 (디버깅용)
            Console.WriteLine($"최대 UBO 크기: {GetMaxUniformBufferSize()} 바이트");

            // 안개 UBO 크기 계산
            int fogUboSize = 48;

            // 카메라 UBO 크기 계산
            int cameraUboSize = SIZE_MAT4 + SIZE_MAT4 + SIZE_VEC3;

            // 안개 UBO 초기화
            _halfPlaneFogUBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.UniformBuffer, _halfPlaneFogUBO);
            Gl.BufferData(BufferTarget.UniformBuffer, (uint)fogUboSize, IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.UniformBuffer, HALFPLANE_FOG_UBO_BINDING_POINT, _halfPlaneFogUBO);

            // 카메라 UBO 초기화
            _cameraUBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.UniformBuffer, _cameraUBO);
            Gl.BufferData(BufferTarget.UniformBuffer, (uint)cameraUboSize, IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.UniformBuffer, CAMERA_UBO_BINDING_POINT, _cameraUBO);

            // 거리 기반 안개 UBO 초기화, 안개 UBO 크기 계산 (std140 레이아웃 규칙 준수)
            int distanceFogUboSize = SIZE_VEC3 + SIZE_FLOAT + SIZE_FLOAT + SIZE_INT + SIZE_FLOAT; // 32바이트
            _distanceFogUBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.UniformBuffer, _distanceFogUBO);
            Gl.BufferData(BufferTarget.UniformBuffer, (uint)distanceFogUboSize, IntPtr.Zero, BufferUsage.DynamicDraw);
            Gl.BindBufferBase(BufferTarget.UniformBuffer, DISTANCE_FOG_UBO_BINDING_POINT, _distanceFogUBO);

            // UBO 바인딩 해제
            Gl.BindBuffer(BufferTarget.UniformBuffer, 0);

            Console.WriteLine("UBO 초기화 완료: 안개 UBO 크기 = " + fogUboSize + ", 카메라 UBO 크기 = " + cameraUboSize);
        }

        /// <summary>
        /// 거리 기반 안개 데이터를 업데이트합니다.
        /// </summary>
        public static void UpdateDistanceFogData(Vertex3f distFogCenter,
                                                float distFogMinRadius,
                                                float distFogMaxRadius,
                                                bool distFogEnabled)
        {
            Gl.BindBuffer(BufferTarget.UniformBuffer, _distanceFogUBO);

            byte[] buffer = new byte[32]; // 필요한 크기로 조정
            int offset = 0;

            // 1) vec3 distFogCenter + padding (0-15 바이트)
            offset = CopyVec3(buffer, offset, distFogCenter);

            // 2) float distFogMinRadius (16-19 바이트)
            offset = CopyFloat(buffer, offset, distFogMinRadius);

            // 3) float distFogMaxRadius (20-23 바이트)
            offset = CopyFloat(buffer, offset, distFogMaxRadius);

            // 4) int distFogEnabled (24-27 바이트)
            offset = CopyInt(buffer, offset, distFogEnabled ? 1 : 0);

            // 5) 패딩 (28-31 바이트)
            offset = AddPadding(offset, 4);

            // 전체 버퍼 업데이트
            Gl.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, (uint)offset, buffer);
            Gl.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        /// <summary>
        /// 안개 관련 데이터를 업데이트합니다.
        /// </summary>
        public static void UpdateHalfPlaneFogData(Vertex3f fogColor, float fogDensity,
                         Vertex4f fogPlane, bool isFogEnabled)
        {
            Gl.BindBuffer(BufferTarget.UniformBuffer, _halfPlaneFogUBO);

            byte[] buffer = new byte[48]; // 전체 UBO 크기
            int offset = 0;

            // 1) vec4 fogPlane (0-15 바이트)
            offset = CopyVec4(buffer, offset, fogPlane);

            // 2) vec3 fogColor + padding (16-31 바이트)
            offset = CopyVec3(buffer, offset, fogColor);

            // 3) float fogDensity (32-35 바이트)
            offset = CopyFloat(buffer, offset, fogDensity);

            // 4) int isFogEnabled (36-39 바이트)
            offset = CopyInt(buffer, offset, isFogEnabled ? 1 : 0);

            // 5) 나머지 패딩 (40-47 바이트)
            offset = AddPadding(offset, 8);

            // 전체 버퍼 업데이트
            Gl.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, (uint)offset, buffer);
            Gl.BindBuffer(BufferTarget.UniformBuffer, 0);
        }


        /// <summary>
        /// 카메라 관련 데이터를 업데이트합니다.
        /// </summary>
        public static void UpdateCameraData(Matrix4x4f viewMatrix, Matrix4x4f projMatrix, Vertex3f cameraPosition)
        {
            Gl.BindBuffer(BufferTarget.UniformBuffer, _cameraUBO);

            byte[] buffer = new byte[SIZE_MAT4 + SIZE_MAT4 + SIZE_VEC3];
            int offset = 0;

            // 함수 호출로 데이터 복사
            offset = CopyMat4(buffer, offset, viewMatrix);
            offset = CopyMat4(buffer, offset, projMatrix);
            offset = CopyVec3(buffer, offset, cameraPosition);

            Gl.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, (uint)offset, buffer);
            Gl.BindBuffer(BufferTarget.UniformBuffer, 0);
        }

        /// <summary>
        /// 셰이더 프로그램에 UBO를 연결합니다.
        /// </summary>
        public static void BindUBOsToShader(uint programID)
        {
            // 안개 UBO 연결
            uint halfplaneFogBlockIndex = Gl.GetUniformBlockIndex(programID, UBOBlockName.HalfFogUniforms.ToString());
            if (halfplaneFogBlockIndex != Gl.INVALID_INDEX)
                Gl.UniformBlockBinding(programID, halfplaneFogBlockIndex, HALFPLANE_FOG_UBO_BINDING_POINT);

            // 카메라 UBO 연결
            /*
            uint cameraBlockIndex = Gl.GetUniformBlockIndex(programID, UBOBlockName.CameraUniforms.ToString());
            if (cameraBlockIndex != Gl.INVALID_INDEX)
                Gl.UniformBlockBinding(programID, cameraBlockIndex, CAMERA_UBO_BINDING_POINT);
            */

            // 거리 기반 안개 UBO 연결
            uint distanceFogBlockIndex = Gl.GetUniformBlockIndex(programID, UBOBlockName.DistanceFogUniforms.ToString());
            if (distanceFogBlockIndex != Gl.INVALID_INDEX)
                Gl.UniformBlockBinding(programID, distanceFogBlockIndex, DISTANCE_FOG_UBO_BINDING_POINT);
        }

        /// <summary>
        /// UBO 리소스를 정리합니다.
        /// </summary>
        public static void Cleanup()
        {
            if (_halfPlaneFogUBO != 0)
            {
                Gl.DeleteBuffers(1, _halfPlaneFogUBO);
                _halfPlaneFogUBO = 0;
            }

            if (_cameraUBO != 0)
            {
                Gl.DeleteBuffers(1, _cameraUBO);
                _cameraUBO = 0;
            }

            if (_distanceFogUBO != 0)
            {
                Gl.DeleteBuffers(1, _distanceFogUBO);
                _distanceFogUBO = 0;
            }
        }

        #region 자료형 복사 헬퍼 메서드

        /// <summary>
        /// 현재 GPU에서 지원하는 Uniform Buffer Object의 최대 크기를 반환합니다.
        /// </summary>
        /// <returns>바이트 단위의 최대 UBO 크기</returns>
        private static int GetMaxUniformBufferSize()
        {
            int maxUniformBlockSize = 0;
            Gl.GetInteger<int>(GetPName.MaxUniformBlockSize, out maxUniformBlockSize);
            return maxUniformBlockSize;
        }

        /// <summary>
        /// 버퍼에 vec3 값을 복사합니다.
        /// </summary>
        private static int CopyVec3(byte[] buffer, int offset, Vertex3f value)
        {
            Buffer.BlockCopy(new float[] { value.x, value.y, value.z, 0.0f }, 0, buffer, offset, SIZE_VEC3);
            return offset + SIZE_VEC3;
        }

        /// <summary>
        /// 버퍼에 vec4 값을 복사합니다.
        /// </summary>
        private static int CopyVec4(byte[] buffer, int offset, Vertex4f value)
        {
            Buffer.BlockCopy(new float[] { value.x, value.y, value.z, value.w }, 0, buffer, offset, SIZE_VEC4);
            return offset + SIZE_VEC4;
        }

        /// <summary>
        /// 버퍼에 float 값을 복사합니다.
        /// </summary>
        private static int CopyFloat(byte[] buffer, int offset, float value)
        {
            Buffer.BlockCopy(new float[] { value }, 0, buffer, offset, SIZE_FLOAT);
            return offset + SIZE_FLOAT;
        }

        /// <summary>
        /// 버퍼에 int 값을 복사합니다.
        /// </summary>
        private static int CopyInt(byte[] buffer, int offset, int value)
        {
            Buffer.BlockCopy(new int[] { value }, 0, buffer, offset, SIZE_INT);
            return offset + SIZE_INT;
        }

        /// <summary>
        /// 버퍼에 bool 값을 복사합니다.
        /// </summary>
        private static int CopyBool(byte[] buffer, int offset, bool value)
        {
            return CopyInt(buffer, offset, value ? 1 : 0);
        }

        /// <summary>
        /// 버퍼에 mat4 값을 복사합니다.
        /// </summary>
        private static int CopyMat4(byte[] buffer, int offset, Matrix4x4f value)
        {
            float[] matData = MatrixToFloatArray(value);
            Buffer.BlockCopy(matData, 0, buffer, offset, SIZE_MAT4);
            return offset + SIZE_MAT4;
        }

        /// <summary>
        /// 버퍼에 패딩을 추가합니다.
        /// </summary>
        private static int AddPadding(int offset, int paddingSize)
        {
            return offset + paddingSize;
        }

        /// <summary>
        /// 행렬을 float 배열로 변환합니다.
        /// </summary>
        private static float[] MatrixToFloatArray(Matrix4x4f matrix)
        {
            float[] result = new float[16];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    result[i * 4 + j] = matrix[(uint)i, (uint)j];
                }
            }
            return result;
        }

        #endregion
    }
}
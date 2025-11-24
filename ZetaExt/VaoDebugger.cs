using System;
using System.Collections.Generic;
using OpenGL;

namespace ZetaExt
{
    /// <summary>
    /// VAO(Vertex Array Object)의 설정 상태를 디버깅하고 출력하는 유틸리티 클래스
    /// </summary>
    public static class VaoDebugger
    {
        /// <summary>
        /// VAO의 설정 상태를 상세히 출력합니다.
        /// </summary>
        /// <param name="vao">검사할 VAO ID</param>
        /// <param name="name">VAO의 이름 (로그 출력용)</param>
        public static void PrintConfiguration(uint vao, string name = "VAO")
        {
            Console.WriteLine($"\n=== {name} Configuration (ID: {vao}) ===");

            Gl.BindVertexArray(vao);

            // VAO에 연결된 Element Array Buffer 확인
            int elementBuffer = 0;
            Gl.Get(GetPName.ElementArrayBufferBinding, out elementBuffer);
            Console.WriteLine($"Element Array Buffer: {elementBuffer}");

            // 활성화된 최대 애트리뷰트 인덱스 확인
            int maxAttribs = 0;
            Gl.Get(GetPName.MaxVertexAttribs, out maxAttribs);

            Console.WriteLine($"\nVertex Attributes (Max: {maxAttribs}):");
            Console.WriteLine("Index | Enabled | Size | Type       | Normalized | Stride | Offset | VBO");
            Console.WriteLine("------|---------|------|------------|------------|--------|--------|--------");

            // 활성화된 애트리뷰트 정보 출력
            for (uint i = 0; i < 16; i++) // 일반적으로 처음 16개만 확인
            {
                if (!TryPrintAttributeInfo(i))
                    continue;
            }

            // 연결된 VBO들의 상세 정보 출력
            PrintVboDetails();

            // Element Buffer 상세 정보 출력
            PrintElementBufferDetails(elementBuffer);

            // 바인딩 해제
            Gl.BindVertexArray(0);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

            Console.WriteLine("========================================\n");
        }

        /// <summary>
        /// 특정 애트리뷰트의 정보를 출력합니다.
        /// </summary>
        /// <param name="index">애트리뷰트 인덱스</param>
        /// <returns>애트리뷰트가 활성화되어 있으면 true</returns>
        private static bool TryPrintAttributeInfo(uint index)
        {
            try
            {
                // 애트리뷰트 활성화 여부
                int enabled = 0;
                Gl.GetVertexAttrib(index, (int)VertexAttribEnum.VertexAttribArrayEnabled, out enabled);

                //Console.WriteLine($"Debug: Attribute {index}, Enabled = {enabled}"); // 디버그 출력

                if (enabled == 0) return false; // 비활성화된 애트리뷰트는 스킵

                // 애트리뷰트 크기 (1, 2, 3, 4)
                int size = 0;
                Gl.GetVertexAttrib(index, (int)VertexAttribEnum.VertexAttribArraySize, out size);

                // 애트리뷰트 타입
                int type = 0;
                Gl.GetVertexAttrib(index, (int)VertexAttribEnum.VertexAttribArrayType, out type);
                string typeName = GetVertexAttribTypeName(type);

                // 정규화 여부
                int normalized = 0;
                Gl.GetVertexAttrib(index, (int)VertexAttribEnum.VertexAttribArrayNormalized, out normalized);

                // Stride
                int stride = 0;
                Gl.GetVertexAttrib(index, (int)VertexAttribEnum.VertexAttribArrayStride, out stride);

                // VBO 바인딩
                int vbo = 0;
                Gl.GetVertexAttrib(index, (int)VertexAttribEnum.VertexAttribArrayBufferBinding, out vbo);

                // Pointer (오프셋)
                IntPtr pointer = IntPtr.Zero;
                Gl.GetVertexAttribPointer(index, 34373, out pointer);

                Console.WriteLine($"{index,5} | {(enabled != 0 ? "YES" : "NO "),7} | {size,4} | {typeName,-10} | {(normalized != 0 ? "YES" : "NO "),10} | {stride,6} | {pointer.ToInt64(),6} | {vbo,6}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting attribute {index}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 연결된 VBO들의 상세 정보를 출력합니다.
        /// </summary>
        private static void PrintVboDetails()
        {
            Console.WriteLine("\nVBO Details:");
            HashSet<int> uniqueVBOs = new HashSet<int>();

            // 활성화된 애트리뷰트에서 사용 중인 VBO 수집
            for (uint i = 0; i < 16; i++)
            {
                int enabled = 0;
                Gl.GetVertexAttrib(i, (int)VertexAttribEnum.VertexAttribArrayEnabled, out enabled);

                if (enabled != 0)
                {
                    int vbo = 0;
                    Gl.GetVertexAttrib(i, (int)VertexAttribEnum.VertexAttribArrayBufferBinding, out vbo);

                    if (vbo != 0)
                    {
                        uniqueVBOs.Add(vbo);
                    }
                }
            }

            // 각 VBO의 상세 정보 출력
            foreach (int vbo in uniqueVBOs)
            {
                Gl.BindBuffer(BufferTarget.ArrayBuffer, (uint)vbo);

                int size = 0;
                Gl.GetBufferParameter(BufferTarget.ArrayBuffer, 34660, out size); // GL_BUFFER_SIZE

                int usage = 0;
                Gl.GetBufferParameter(BufferTarget.ArrayBuffer, 34661, out usage); // GL_BUFFER_USAGE
                string usageName = GetBufferUsageName(usage);

                Console.WriteLine($"  VBO {vbo}: Size = {size} bytes ({size / 1024.0:F2} KB), Usage = {usageName}");
            }
        }

        /// <summary>
        /// Element Buffer의 상세 정보를 출력합니다.
        /// </summary>
        /// <param name="elementBufferId">Element Buffer ID</param>
        private static void PrintElementBufferDetails(int elementBufferId)
        {
            if (elementBufferId == 0) return;

            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, (uint)elementBufferId);

            int size = 0;
            Gl.GetBufferParameter(BufferTarget.ElementArrayBuffer, 34660, out size); // GL_BUFFER_SIZE

            int usage = 0;
            Gl.GetBufferParameter(BufferTarget.ElementArrayBuffer, 34661, out usage); // GL_BUFFER_USAGE
            string usageName = GetBufferUsageName(usage);

            int indexCount = size / 4; // assuming uint indices
            Console.WriteLine($"\nElement Buffer: Size = {size} bytes ({indexCount} indices), Usage = {usageName}");
        }

        /// <summary>
        /// VertexAttribType enum 값을 문자열로 변환
        /// </summary>
        private static string GetVertexAttribTypeName(int type)
        {
            if (type == (int)VertexAttribType.Byte) return "BYTE";
            if (type == (int)VertexAttribType.UnsignedByte) return "UBYTE";
            if (type == (int)VertexAttribType.Short) return "SHORT";
            if (type == (int)VertexAttribType.UnsignedShort) return "USHORT";
            if (type == (int)VertexAttribType.Int) return "INT";
            if (type == (int)VertexAttribType.UnsignedInt) return "UINT";
            if (type == (int)VertexAttribType.Float) return "FLOAT";
            if (type == (int)VertexAttribType.Double) return "DOUBLE";
            if (type == (int)VertexAttribType.HalfFloat) return "HALF_FLOAT";
            return $"UNKNOWN({type})";
        }

        /// <summary>
        /// BufferUsage enum 값을 문자열로 변환
        /// </summary>
        private static string GetBufferUsageName(int usage)
        {
            if (usage == (int)BufferUsage.StreamDraw) return "STREAM_DRAW";
            if (usage == (int)BufferUsage.StreamRead) return "STREAM_READ";
            if (usage == (int)BufferUsage.StreamCopy) return "STREAM_COPY";
            if (usage == (int)BufferUsage.StaticDraw) return "STATIC_DRAW";
            if (usage == (int)BufferUsage.StaticRead) return "STATIC_READ";
            if (usage == (int)BufferUsage.StaticCopy) return "STATIC_COPY";
            if (usage == (int)BufferUsage.DynamicDraw) return "DYNAMIC_DRAW";
            if (usage == (int)BufferUsage.DynamicRead) return "DYNAMIC_READ";
            if (usage == (int)BufferUsage.DynamicCopy) return "DYNAMIC_COPY";
            return $"UNKNOWN({usage})";
        }
    }
}
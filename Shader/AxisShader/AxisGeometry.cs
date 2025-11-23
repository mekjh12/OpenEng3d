using OpenGL;
using System;

namespace Shader
{
    /// <summary>
    /// 3축 지오메트리 생성
    /// </summary>
    public static class AxisGeometry
    {
        public static uint VAO { get; private set; }
        public static uint VBO { get; private set; }
        public static int VertexCount => 6; // 3축 × 2점 = 6점

        static AxisGeometry()
        {
            CreateGeometry();
        }

        private static void CreateGeometry()
        {
            // 3개 축의 선분 + 색상 정의
            float[] vertexData = {
            // Position (x,y,z)     Color (r,g,b,a)
            // x축: (0,0,0) -> (1,0,0) - 빨강
            0.0f, 0.0f, 0.0f,      1.0f, 0.0f, 0.0f, 1.0f,
            1.0f, 0.0f, 0.0f,      1.0f, 0.0f, 0.0f, 1.0f,
            
            // Y축: (0,0,0) -> (0,1,0) - 초록  
            0.0f, 0.0f, 0.0f,      0.0f, 1.0f, 0.0f, 1.0f,
            0.0f, 1.0f, 0.0f,      0.0f, 1.0f, 0.0f, 1.0f,
            
            // Z축: (0,0,0) -> (0,0,1) - 파랑
            0.0f, 0.0f, 0.0f,      0.0f, 0.0f, 1.0f, 1.0f,
            0.0f, 0.0f, 1.0f,      0.0f, 0.0f, 1.0f, 1.0f
        };

            VAO = Gl.GenVertexArray();
            VBO = Gl.GenBuffer();

            Gl.BindVertexArray(VAO);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            Gl.BufferData(BufferTarget.ArrayBuffer, (uint)(vertexData.Length * sizeof(float)), vertexData, BufferUsage.StaticDraw);

            // Position 속성 (location = 0)
            Gl.VertexAttribPointer(0, 3, VertexAttribType.Float, false, 7 * sizeof(float), IntPtr.Zero);
            Gl.EnableVertexAttribArray(0);

            // Color 속성 (location = 1)
            Gl.VertexAttribPointer(1, 4, VertexAttribType.Float, false, 7 * sizeof(float), new IntPtr(3 * sizeof(float)));
            Gl.EnableVertexAttribArray(1);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);
            Gl.BindVertexArray(0);
        }

        public static void Cleanup()
        {
            Gl.DeleteVertexArrays(new uint[] { VAO });
            Gl.DeleteBuffers(VBO);
        }
    }
}

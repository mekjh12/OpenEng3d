using Camera3d;
using OpenGL;
using Shader;
using System;
using ZetaExt;

namespace Cloud
{
    public class WorleyNoise
    {
        /// <summary>
        /// Worley노이즈를 생성하여 GPU에 올리고 SSBO를 반환한다.
        /// </summary>
        /// <param name="numCellsPerAxis">가로,세로,높이를 나눌 셀의 갯수</param>
        /// <param name="bufferName"></param>
        /// <returns></returns>
        public static uint CreateWorleyPointsBuffer(uint numCellsPerAxis, string bufferName)
        {
            uint ssbo = 0;

            Vertex3f[] points = new Vertex3f[numCellsPerAxis * numCellsPerAxis * numCellsPerAxis];
            float cellSize = 1.0f / (float)numCellsPerAxis;
            for (int x = 0; x < numCellsPerAxis; x++)
            {
                for (int y = 0; y < numCellsPerAxis; y++)
                {
                    for (int z = 0; z < numCellsPerAxis; z++)
                    {
                        Vertex3f randomOffset = Rand.NextVectorFromZeroToOne;
                        Vertex3f position = (new Vertex3f(x, y, z) + randomOffset) * cellSize;
                        int index = (int)(x + numCellsPerAxis * (y + z * numCellsPerAxis));
                        points[index] = position;
                    }
                }
            }

            uint ssboSize = (uint)(points.Length * Vertex3f.Size);
            ssbo = Gl.CreateBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer, ssboSize, points, BufferUsage.DynamicCopy);
            Gl.BufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, ssboSize, points);
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);

            return ssbo;
        }

    }
}
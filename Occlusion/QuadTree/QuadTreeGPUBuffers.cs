using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Occlusion
{
    /// <summary>
    /// QuadTree 관련 GPU 버퍼 관리
    /// 리프 노드별 ObjectID 리스트를 GPU로 전송
    /// </summary>
    public class QuadTreeGPUBuffers : IDisposable
    {
        private uint _leafObjectIDsSSBO;
        private uint _leafInfoStartIndexSSBO;
        private uint _leafInfoCountSSBO;
        private uint _visibleLeafIDsSSBO;

        private int _totalLeafNodes;
        private int _maxVisibleLeafs;

        public uint LeafObjectIDsSSBO => _leafObjectIDsSSBO;
        public uint LeafInfoStartIndexSSBO => _leafInfoStartIndexSSBO;
        public uint LeafInfoCountSSBO => _leafInfoCountSSBO;
        public uint VisibleLeafIDsSSBO => _visibleLeafIDsSSBO;

        public void Initialize(QuadTreeEx tree)
        {
            Console.WriteLine("  QuadTree GPU 버퍼 초기화 중...");
            _totalLeafNodes = tree.LeafNodes.Count;
            _maxVisibleLeafs = _totalLeafNodes;

            uint[][] leafObjectIDs = tree.CreateLeafObjectIDArrays();

            List<uint> flatObjectIDs = new List<uint>();
            List<uint> startIndices = new List<uint>();
            List<uint> counts = new List<uint>();

            for (int i = 0; i < leafObjectIDs.Length; i++)
            {
                startIndices.Add((uint)flatObjectIDs.Count);
                counts.Add((uint)leafObjectIDs[i].Length);
                flatObjectIDs.AddRange(leafObjectIDs[i]);
            }

            Console.WriteLine($"    총 리프: {leafObjectIDs.Length}개");
            Console.WriteLine($"    총 ObjectID: {flatObjectIDs.Count}개");

            CreateLeafObjectIDsSSBO(flatObjectIDs.ToArray());
            CreateLeafInfoSSBOs(startIndices.ToArray(), counts.ToArray());
            CreateVisibleLeafIDsSSBO();

            Console.WriteLine("  QuadTree GPU 버퍼 초기화 완료!");
        }

        private void CreateLeafObjectIDsSSBO(uint[] objectIDs)
        {
            _leafObjectIDsSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _leafObjectIDsSSBO);

            uint sizeInBytes = (uint)(objectIDs.Length * sizeof(uint));
            Gl.BufferData(
                BufferTarget.ShaderStorageBuffer,
                sizeInBytes,
                objectIDs,
                BufferUsage.StaticDraw
            );

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        private void CreateLeafInfoSSBOs(uint[] startIndices, uint[] counts)
        {
            // StartIndex SSBO
            _leafInfoStartIndexSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _leafInfoStartIndexSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(startIndices.Length * sizeof(uint)),
                startIndices, BufferUsage.StaticDraw);

            // Count SSBO
            _leafInfoCountSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _leafInfoCountSSBO);
            Gl.BufferData(BufferTarget.ShaderStorageBuffer,
                (uint)(counts.Length * sizeof(uint)),
                counts, BufferUsage.StaticDraw);

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        private void CreateVisibleLeafIDsSSBO()
        {
            _visibleLeafIDsSSBO = Gl.GenBuffer();
            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, _visibleLeafIDsSSBO);

            uint sizeInBytes = (uint)(_maxVisibleLeafs * sizeof(uint));
            Gl.BufferData(
                BufferTarget.ShaderStorageBuffer,
                sizeInBytes,
                IntPtr.Zero,
                BufferUsage.DynamicDraw
            );

            Gl.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        public void Dispose()
        {
            if (_leafObjectIDsSSBO != 0) Gl.DeleteBuffers(_leafObjectIDsSSBO);
            if (_leafInfoStartIndexSSBO != 0) Gl.DeleteBuffers(_leafInfoStartIndexSSBO);
            if (_leafInfoCountSSBO != 0) Gl.DeleteBuffers(_leafInfoCountSSBO);
            if (_visibleLeafIDsSSBO != 0) Gl.DeleteBuffers(_visibleLeafIDsSSBO);
        }
    }
}
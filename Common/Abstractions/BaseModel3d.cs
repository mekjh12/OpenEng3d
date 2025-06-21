using OpenGL;
using System;

namespace Common.Abstractions
{
    /// <summary>
    /// CPU에 기본적 정보만 갖는 기본모델이다.
    /// </summary>
    public abstract class BaseModel3d: IDisposable
    {
        protected readonly string _name;

        protected int _vertexCount;
        protected int _indexCount;

        protected uint _vao;

        protected bool _isDrawElement;
        protected bool _isCpuStored;

        protected Vertex3f[] _vertices;

        public int IndexCount
        {
            get => _indexCount;
        }

        public bool IsDrawElement
        {
            get => _isDrawElement;
            set => _isDrawElement = value;
        }

        public uint VAO => _vao;

        public int VertexCount
        {
            get => _vertexCount;
            set => _vertexCount = value;
        }

        protected BaseModel3d(bool isCpuStored = true, bool isDrawElement = false)
        {
            _name = "";
            _isCpuStored = isCpuStored;
            _isDrawElement = isDrawElement;
        }

        protected BaseModel3d(Vertex3f[] vertices, bool isCpuStored = true, bool isDrawElement = false)
        {
            _name = "";
            _vertices = vertices;
            _isCpuStored = isCpuStored;
            _isDrawElement = isDrawElement;
            _vertexCount = _vertices.Length;
        }

        public abstract void Dispose();
    }
}
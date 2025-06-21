using OpenGL;
using System;
using ZetaExt;

namespace Common.Abstractions
{
    /// <summary>
    /// 물체의 기본적인 이름, 색상만 가진다.
    /// </summary>
    public abstract class BaseEntity : IDisposable, IBaseEntity
    {
        protected string _name;
        private Vertex3f _color;
        protected uint _guid = 0;

        protected BaseEntity(string name)
        {
            _guid = Core.GUID.GenID;
            _name = (name == "") ? $"Entity" + _guid : name;
            _color = Rand.NextColor3f;
        }

        public uint OBJECT_GUID
        {
            get => _guid;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public Vertex3f Color
        {
            get => _color;
            set => _color = value;
        }

        public abstract void Dispose();
        public abstract void Update(Camera camera);
    }
}

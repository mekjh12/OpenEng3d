using OpenGL;

namespace Common.Abstractions
{
    public abstract class Light
    {
        protected Vertex3f _direction;
        protected Color4 _color;
        protected float _intensity;

        public virtual Vertex3f GetDirection() => _direction;
        public virtual void SetDirection(Vertex3f dir) => _direction = dir.Normalized;
    }

}

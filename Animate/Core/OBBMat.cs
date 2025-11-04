using OpenGL;
using ZetaExt;

namespace Animate
{
    public class OBBMat
    {
        protected Matrix4x4f _obb;
        private Vertex4f _color;
        protected Matrix4x4f _model;

        public Matrix4x4f ModelMatrix
        {
            get
            {
                _model[0, 0] = _obb[0, 0] * 0.5f;
                _model[0, 1] = _obb[0, 1] * 0.5f;
                _model[0, 2] = _obb[0, 2] * 0.5f;
                _model[1, 0] = _obb[1, 0] * 0.5f;
                _model[1, 1] = _obb[1, 1] * 0.5f;
                _model[1, 2] = _obb[1, 2] * 0.5f;
                _model[2, 0] = _obb[2, 0] * 0.5f;
                _model[2, 1] = _obb[2, 1] * 0.5f;
                _model[2, 2] = _obb[2, 2] * 0.5f;

                _model[3, 0] = _model[0, 0] + _model[1, 0] + _model[2, 0] + _obb[3, 0];
                _model[3, 1] = _model[0, 1] + _model[1, 1] + _model[2, 1] + _obb[3, 1];
                _model[3, 2] = _model[0, 2] + _model[1, 2] + _model[2, 2] + _obb[3, 2];

                return _model;
            }
        }

        public Vertex4f Color { get => _color; set => _color = value; }
        public float Alpha { get => _color.w; set => _color.w = value; }

        public OBBMat()
        {
            _obb = Matrix4x4f.Identity;
            _model = Matrix4x4f.Identity;
            _color = new Vertex4f(1f, 1f, 1f, 1f);
            _color.x = Rand.NextFloat;
            _color.y = Rand.NextFloat;
            _color.z = Rand.NextFloat;
        }

        public OBBMat(Vertex3f a,  Vertex3f b, Vertex3f c, Vertex3f pos)
        {
            _obb = Matrix4x4f.Identity;
            _model = Matrix4x4f.Identity;

            _obb[0, 0] = a.x;
            _obb[0, 1] = a.y;
            _obb[0, 2]  = a.z;

            _obb[1, 0] = b.x;
            _obb[1, 1] = b.y;
            _obb[1, 2] = b.z;

            _obb[2, 0] = c.x;
            _obb[2, 1] = c.y;
            _obb[2, 2] = c.z;

            _obb[3, 0] = pos.x;
            _obb[3, 1] = pos.y;
            _obb[3, 2] = pos.z;
            _obb[3, 3] = 1f;

            _color = new Vertex4f(1f, 1f, 1f, 1f);
            _color.x = Rand.NextFloat;
            _color.y = Rand.NextFloat;
            _color.z = Rand.NextFloat;
        }
        
        public void SetColumn(uint index, Vertex3f vec)
        {
            _obb[index, 0] = vec.x;
            _obb[index, 1] = vec.y;
            _obb[index, 2] = vec.z;
        }
    }
}

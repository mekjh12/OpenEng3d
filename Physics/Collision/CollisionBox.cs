using FastMath;
using OpenGL;
using System;
using ZetaExt;

namespace Physics.Collision
{
    /// <summary>
    /// 충돌 구
    /// </summary>
    public class CollisionBox: CollisionPrimitive
    {
        protected Vertex3f _halfSize;

        /// <summary>
        /// 박스의 반의 크기
        /// </summary>
        public Vertex3f HalfSize
        {
            get => _halfSize;
            set => _halfSize = value;
        }

        /// <summary>
        /// 상자의 질량중심으로부터의 x축 월드벡터
        /// </summary>
        public Vertex3f Axisx => _body.TransformMatrix.Column0.xyz().Normalized;

        /// <summary>
        /// 상자의 질량중심으로부터의 Y축 월드벡터
        /// </summary>
        public Vertex3f AxisY => _body.TransformMatrix.Column1.xyz().Normalized;

        /// <summary>
        /// 상자의 질량중심으로부터의 Z축 월드벡터
        /// </summary>
        public Vertex3f AxisZ => _body.TransformMatrix.Column2.xyz().Normalized;

        /// <summary>
        /// 축을 가져온다. 0,1,2는 순서대로 x,y,z축이다.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Vertex3f Axis(int index)
        {
            if (index == 0) return Axisx;
            else if (index == 1) return AxisY;
            else if (index == 2) return AxisZ;
            else return Vertex3f.Zero;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="position"></param>
        /// <param name="halfSize"></param>
        public CollisionBox(Vertex3f position, Vertex3f halfSize) : base(position)
        {
            _halfSize = halfSize;
        }

        /// <summary>
        /// 상자를 완전히 감싼 구의 반지름을 가져온다. <br/>
        /// 상자를 축에 사영한 길이를 반환한다.
        /// </summary>
        /// <param name="axis">축벡터</param>
        /// <returns></returns>
        public float TransformToAxis(Vertex3f axis)
        {
            Vertex3f unitAxis = axis.Normalized;
            return _halfSize.x * MathFast.Abs(unitAxis * Axisx) +
                   _halfSize.y * MathFast.Abs(unitAxis * AxisY) +
                   _halfSize.z * MathFast.Abs(unitAxis * AxisZ);
        }

        /// <summary>
        /// 상자를 축에 사영한 길이의 반지름을 반환한다.
        /// </summary>
        /// <param name="axis"></param>
        /// <returns></returns>
        public float BoxRadius(Vertex3f axis)
        {
            Vertex3f unitAxis = axis.Normalized;
            return _halfSize.x * MathFast.Abs(unitAxis * Axisx) +
                   _halfSize.y * MathFast.Abs(unitAxis * AxisY) +
                   _halfSize.z * MathFast.Abs(unitAxis * AxisZ);
        }

    }
}

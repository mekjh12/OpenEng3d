using OpenGL;
using ZetaExt;

namespace Physics.Collision
{
    /// <summary>
    /// 충돌 구
    /// </summary>
    public class CollisionPlane : CollisionPrimitive
    {
        Vertex3f _normal;
        float _distance;

        /// <summary>
        /// 평면의 법선 벡터
        /// </summary>
        public Vertex3f Normal
        {
            get => _normal;
            set => _normal = value.Normalized;
        }

        /// <summary>
        /// 평면의 원점까지의 부호를 갖는 거리
        /// </summary>
        public float Distance
        {
            get => _distance;
            set => _distance = value;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public CollisionPlane(Vertex3f position) : base(position)
        {

        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="position"></param>
        public CollisionPlane(Vertex3f normal, Vertex3f position) : base(position)
        {
            _normal = normal;
            _distance = _position.Dot(_normal);
        }

        /// <summary>
        /// 평면으로부터 점까지의 부호를 갖는 거리를 반환한다.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public float SignedDistance(Vertex3f point)
        {
            return (_normal * point) - _distance;
        }
    }
}

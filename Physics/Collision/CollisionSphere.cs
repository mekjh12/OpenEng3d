using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics.Collision
{
    /// <summary>
    /// 충돌 구
    /// </summary>
    public class CollisionSphere : CollisionPrimitive
    {
        /// <summary>
        /// 반지름
        /// </summary>
        protected float _radius;

        /// <summary>
        /// 구의 반지름
        /// </summary>
        public float Radius
        {
            get => _radius;
            set => _radius = value;
        }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="position"></param>
        public CollisionSphere(Vertex3f position) : base(position)
        {

        }

    }
}

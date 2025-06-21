using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Physics.Collision
{
    /// <summary>
    /// 충돌 원형
    /// </summary>
    public class CollisionPrimitive
    {
        protected RigidBody _body;
        protected Matrix4x4f _offset = Matrix4x4f.Identity; // 월드 공간에서의 모델행렬
        protected Vertex3f _position;

        /// <summary>
        /// 충돌 원형의 월드 공간의 위치
        /// </summary>
        public Vertex3f Position
        {
            get => _position; 
            set => _position = value;
        }

        /// <summary>
        /// 보관한 강체
        /// </summary>
        public RigidBody RigidBody
        {
            get => _body;
            set => _body = value;
        }

        /// <summary>
        /// 월드 공간에서의 모델 행렬
        /// </summary>
        public Matrix4x4f Offset
        {
            get => _offset; 
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public CollisionPrimitive(Vertex3f position)
        {
            _position = position;
        }
    }
}

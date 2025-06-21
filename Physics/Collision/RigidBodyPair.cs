using System;

namespace Physics.Collision
{
    /// <summary>
    /// 충돌 감지된 두 강체를 쌍으로 담는 구조체
    /// </summary>
    public struct RigidBodyPair
    {
        private readonly RigidBody _rigidA;      // 첫 번째 강체
        private readonly RigidBody _rigidB;      // 두 번째 강체

        /// <summary>
        /// 첫 번째 강체 읽기 프로퍼티
        /// </summary>
        public RigidBody RigidBodyA => _rigidA;

        /// <summary>
        /// 두 번째 강체 읽기 프로퍼티
        /// </summary>
        public RigidBody RigidBodyB => _rigidB;

        /// <summary>
        /// 강체 쌍을 생성한다.
        /// </summary>
        public RigidBodyPair(RigidBody a, RigidBody b)
        {
            _rigidA = a ?? throw new ArgumentNullException(nameof(a));
            _rigidB = b ?? throw new ArgumentNullException(nameof(b));
        }
    }
}

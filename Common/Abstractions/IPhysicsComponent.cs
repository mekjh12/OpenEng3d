using OpenGL;

namespace Common.Abstractions
{
    // 물리/충돌 관련 인터페이스
    public interface IPhysicsComponent
    {
        BoundVolume RigidBody { get; }
        bool IsVisibleRigidBody { get; set; }
        bool IsCollisionTest { get; set; }
        void SetRigidBody(Vertex3f tightedVector, Vertex3f translated);
    }
}

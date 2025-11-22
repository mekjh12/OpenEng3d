using Common;
using OpenGL;

namespace Occlusion
{
    /// <summary>
    /// 월드에 저장될 객체 (나무, 바위 등)
    /// </summary>
    public class WorldObject
    {
        public AABB3f AABB;
        public int ObjectID;

        // 경계 플래그들
        public bool OnBoundary = false;          // 경계에 걸쳐있는가?
        public byte BoundaryFlags = 0;           // 어느 경계인가? (bit flags)

        // 비트 플래그 정의 (Z-up 버전)
        public const byte BOUNDARY_NONE = 0;
        public const byte BOUNDARY_X_MIN = 1 << 0;  // 서쪽 경계
        public const byte BOUNDARY_X_MAX = 1 << 1;  // 동쪽 경계
        public const byte BOUNDARY_Y_MIN = 1 << 2;  // 남쪽 경계 (Z-up에서 Y가 수평)
        public const byte BOUNDARY_Y_MAX = 1 << 3;  // 북쪽 경계

        public WorldObject(AABB3f aabb, int id)
        {
            AABB = aabb;
            ObjectID = id;
        }
    }
}

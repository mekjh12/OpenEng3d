using System.Runtime.InteropServices;

namespace Renderer
{
    // ============================================================
    // GPU-Driven 구조체들
    // ============================================================

    /// <summary>
    /// 활성 타일 정보 (동적, 9~49개)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ActiveTileData
    {
        public float WorldX;      // 타일 월드 시작 위치
        public float WorldY;
        public float TileSize;
        public float _padding;
        // 16 bytes = vec4
    }

    /// <summary>
    /// 로컬 템플릿 구조체 (타일당 9801개, 고정)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct GrassLocalTemplate
    {
        public float LocalX;      // 0 ~ 10
        public float LocalY;      // 0 ~ 10
        public float Rotation;
        public float Scale;
        // 16 bytes = vec4
    }

    /// <summary>
    /// 후보 타일 데이터 (Compute Shader 입력, 121개)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct CandidateTileData
    {
        public float WorldX;      // 타일 월드 시작 위치
        public float WorldY;
        public float MinZ;        // AABB 최소 높이 (0)
        public float MaxZ;        // AABB 최대 높이 (200)
                                  // 16 bytes = vec4
    }

    /// <summary>
    /// 가시 타일 데이터 (Compute Shader 출력)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VisibleTileData
    {
        public float WorldX;      // 타일 월드 시작 위치
        public float WorldY;
        public float TileSize;    // 10.0
        public float LOD;         // 0.0, 1.0, 2.0 (int를 float로 저장)
                                  // 16 bytes = vec4
    }

    /// <summary>
    /// 4D 벡터 (Plane 표현용)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vector4
    {
        public float x, y, z, w;

        public Vector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
    }

    /// <summary>
    /// Frustum Planes (UBO용, std140 레이아웃)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct FrustumPlanesData
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public Vector4[] Planes;  // xyz = normal, w = distance
    }
}

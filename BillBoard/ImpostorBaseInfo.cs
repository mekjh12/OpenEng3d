using System.Runtime.InteropServices;

namespace BillBoard
{
    /// <summary>
    /// GPU SSBO용 임포스터 기본 정보 (std430 레이아웃)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ImpostorBaseInfo
    {
        // ========== AABB & Bounding 정보 (16 bytes) ==========
        public float AABBCenterX;
        public float AABBCenterY;
        public float AABBCenterZ;
        public float BoundingSphereRadius;

        // ========== AABB Size (16 bytes) ==========
        public float AABBSizeX;
        public float AABBSizeY;
        public float AABBSizeZ;
        public float AtlasUVScale;              // 패딩 대신 여기 배치

        // ========== 아틀라스 정보 (16 bytes) ==========
        public int AtlasSize;
        public int IndividualSize;
        public int HorizontalAngles;
        public int VerticalAngles;

        // ========== 각도 정보 (16 bytes) ==========
        public float VerticalAngleMin;
        public float VerticalAngleMax;
        public int TotalFrames;
        public int _padding1;

        // ========== 텍스처 ID (16 bytes) ==========
        public uint AlbedoTextureID;
        public uint NormalTextureID;
        public uint DepthTextureID;
        public uint _padding2;

        // Total: 80 bytes (std430 정렬)

        /// <summary>
        /// ImpostorBakeResult로부터 SSBO 구조체 생성
        /// </summary>
        public static ImpostorBaseInfo FromBakeResult(ImpostorBakeResult result)
        {
            var metadata = result.Metadata;
            return new ImpostorBaseInfo
            {
                AABBCenterX = metadata.AABBCenter.X,
                AABBCenterY = metadata.AABBCenter.Y,
                AABBCenterZ = metadata.AABBCenter.Z,
                BoundingSphereRadius = metadata.BoundingSphereRadius,

                AABBSizeX = metadata.AABBSize.X,
                AABBSizeY = metadata.AABBSize.Y,
                AABBSizeZ = metadata.AABBSize.Z,
                AtlasUVScale = metadata.AtlasUVScale,

                AtlasSize = metadata.AtlasSize,
                IndividualSize = metadata.IndividualSize,
                HorizontalAngles = metadata.HorizontalAngles,
                VerticalAngles = metadata.VerticalAngles,

                VerticalAngleMin = metadata.VerticalAngleMin,
                VerticalAngleMax = metadata.VerticalAngleMax,
                TotalFrames = metadata.TotalFrames,
                _padding1 = 0,

                AlbedoTextureID = result.AlbedoTextureID,
                NormalTextureID = result.NormalTextureID,
                DepthTextureID = result.DepthTextureID,
                _padding2 = 0
            };
        }
    }

    /// <summary>
    /// 임포스터 인스턴스별 데이터 (변환 행렬 + 인덱스)
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ImpostorInstanceData
    {
        // 월드 위치 + 스케일
        public float PositionX;
        public float PositionY;
        public float PositionZ;
        public float Scale;

        // 추가 정보
        public int ImpostorBaseIndex;           // ImpostorBaseInfo 배열의 인덱스
        public float CustomData1;               // 사용자 정의 (예: 색상 변화)
        public float CustomData2;               // 사용자 정의 (예: 바람 효과)
        public float _padding;

        // Total: 32 bytes
    }
}
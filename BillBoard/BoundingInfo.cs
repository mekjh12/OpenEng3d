using Common;

namespace BillBoard
{
    /// <summary>
    /// 모델의 바운딩 정보를 저장하는 내부 구조체
    /// </summary>
    public struct BoundingInfo
    {
        /// <summary>
        /// 조정된 AABB (하단 피벗 적용됨)
        /// </summary>
        public AABB3f AdjustedAABB;

        /// <summary>
        /// 수평 Bounding Sphere 반경 (XY 평면)
        /// </summary>
        public float HorizontalRadius;

        /// <summary>
        /// 실제 높이 (Z축)
        /// </summary>
        public float Height;

        /// <summary>
        /// 원본 Z 오프셋 (하단 피벗 모드에서 사용)
        /// </summary>
        public float ZOffset;

        /// <summary>
        /// 디버그용 문자열 출력
        /// </summary>
        public override string ToString()
        {
            return $"BoundingInfo[Radius={HorizontalRadius:F2}, Height={Height:F2}, ZOffset={ZOffset:F2}]";
        }
    }
}

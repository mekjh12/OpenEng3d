namespace BillBoard
{
    /// <summary>
    /// 임포스터 베이킹 설정 (단순화 버전)
    /// 하단 피벗 제거, AABB 중심 기준
    /// </summary>
    public struct ImpostorSettings
    {
        public string Name { get; set; }

        // 아틀라스 설정
        public int AtlasSize { get; set; }          // 전체 아틀라스 크기 (예: 1024)
        public int IndividualSize { get; set; }     // 개별 프레임 크기 (예: 128)

        // 각도 설정
        public int HorizontalAngles { get; set; }   // 수평 분할 (예: 8)
        public int VerticalAngles { get; set; }     // 수직 분할 (예: 6)
        public float VerticalAngleMin { get; set; } // 수직 최소 각도 (예: -30°)
        public float VerticalAngleMax { get; set; } // 수직 최대 각도 (예: 60°)

        // 바운딩 설정
        public float PaddingFactor { get; set; }    // AABB 패딩 (예: 0.01 = 1%)

        /// <summary>
        /// 기본 설정 생성
        /// </summary>
        public static ImpostorSettings CreateDefault(string modelName)
        {
            return new ImpostorSettings
            {
                Name = modelName,
                AtlasSize = 1024,
                IndividualSize = 128,
                HorizontalAngles = 8,
                VerticalAngles = 6,
                VerticalAngleMin = -30f,
                VerticalAngleMax = 60f,
                PaddingFactor = 0.01f
            };
        }

        /// <summary>
        /// 고품질 설정 생성
        /// </summary>
        public static ImpostorSettings CreateHighQuality(string modelName)
        {
            return new ImpostorSettings
            {
                Name = modelName,
                AtlasSize = 2048,
                IndividualSize = 128,
                HorizontalAngles = 16,
                VerticalAngles = 16,
                VerticalAngleMin = -30f,
                VerticalAngleMax = 89f,
                PaddingFactor = 0.001f
            };
        }

        /// <summary>
        /// 저품질 설정 생성 (LOD용)
        /// </summary>
        public static ImpostorSettings CreateLowQuality(string modelName)
        {
            return new ImpostorSettings
            {
                Name = modelName,
                AtlasSize = 512,
                IndividualSize = 64,
                HorizontalAngles = 4,
                VerticalAngles = 4,
                VerticalAngleMin = -20f,
                VerticalAngleMax = 50f,
                PaddingFactor = 0.01f
            };
        }
    }
}
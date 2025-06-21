namespace Model3d
{
    /// <summary>
    /// 임포스터 생성 및 렌더링에 필요한 공통 설정값들
    /// </summary>
    /// <remarks>
    /// 이 구조체는 3D 모델의 임포스터를 생성하고 렌더링하는 데 필요한 설정을 관리합니다.
    /// 
    /// 사용법:
    /// 1. 기본 설정으로 인스턴스 생성:
    ///    ImpostorSettings settings = ImpostorSettings.CreateDefault();
    /// 
    /// 2. 사용자 정의 설정으로 인스턴스 생성:
    ///    ImpostorSettings settings = new ImpostorSettings
    ///    {
    ///        AtlasSize = 4096,
    ///        IndividualSize = 512,
    ///        HorizontalAngles = 16,
    ///        VerticalAngles = 8,
    ///        VerticalAngleMin = -30,
    ///        VerticalAngleMax = 90
    ///    };
    /// 
    /// 3. 설정 유효성 검사:
    ///    if (!settings.Validate())
    ///    {
    ///        Console.WriteLine("Invalid impostor settings!");
    ///    }
    /// 
    /// 4. 설정 사용 예시:
    ///    ImpostorGenerator generator = new ImpostorGenerator(settings);
    ///    generator.GenerateImpostor(model);
    /// 
    /// 주의사항:
    /// - AtlasSize는 IndividualSize의 배수여야 합니다.
    /// - AtlasSize / IndividualSize 의 제곱은 HorizontalAngles * VerticalAngles 이상이어야 합니다.
    /// - VerticalAngleMin은 VerticalAngleMax보다 작아야 하며, VerticalAngleMax는 90도를 초과할 수 없습니다.
    /// - HorizontalAngles와 VerticalAngles는 1 이상이어야 합니다.
    /// </remarks>
    public struct ImpostorSettings
    {
        /// <summary>
        /// 전체 아틀라스 텍스처의 해상도
        /// </summary>
        public int AtlasSize { get; set; }

        /// <summary>
        /// 각 방향별 뷰의 텍스처 해상도
        /// </summary>
        public int IndividualSize { get; set; }

        /// <summary>
        /// 수평 방향으로 캡처할 각도 수 (예: 8)
        /// </summary>
        public int HorizontalAngles { get; set; }

        /// <summary>
        /// 수직 방향으로 캡처할 각도 수 (예: 4)
        /// </summary>
        public int VerticalAngles { get; set; }

        /// <summary>
        /// 수직 방향 최소 각도 (하단, 예: 0)
        /// </summary>
        public float VerticalAngleMin { get; set; }

        /// <summary>
        /// 수직 방향 최대 각도 (상단, 예: 60)
        /// </summary>
        public float VerticalAngleMax { get; set; }

        /// <summary>
        /// 기본값으로 설정된 임포스터 설정을 생성합니다.
        /// </summary>
        public static ImpostorSettings CreateDefault()
        {
            return new ImpostorSettings
            {
                AtlasSize = 512,            // 512x512 아틀라스
                IndividualSize = 64,        // 64x64 개별 뷰
                HorizontalAngles = 8,       // 45도 간격으로 8방향
                VerticalAngles = 4,         // 4개의 수직 각도
                VerticalAngleMin = 0,       // 최하단 각도
                VerticalAngleMax = 88       // 최상단 각도
            };
        }

        public static ImpostorSettings CreateSettings(int individualSize = 128, int horizontalAngles = 8, int verticalAngles = 6)
        {
            return new ImpostorSettings
            {
                AtlasSize = individualSize * horizontalAngles,
                IndividualSize = individualSize,
                HorizontalAngles = horizontalAngles,
                VerticalAngles = verticalAngles,
                VerticalAngleMin = -30,               // 최하단 각도
                VerticalAngleMax = 88               // 최상단 각도
            };
        }

        /// <summary>
        /// 설정값의 유효성을 검증합니다.
        /// </summary>
        public bool Validate()
        {
            // 아틀라스 크기는 개별 크기의 배수여야 함
            if (AtlasSize % IndividualSize != 0) return false;

            // 아틀라스는 모든 뷰를 수용할 수 있어야 함
            if ((AtlasSize / IndividualSize) * (AtlasSize / IndividualSize) < HorizontalAngles * VerticalAngles) return false;

            // 각도 범위 검증
            if (VerticalAngleMin >= VerticalAngleMax) return false;
            if (VerticalAngleMax > 90) return false;

            // 각도 수는 최소 1 이상이어야 함
            if (HorizontalAngles < 1 || VerticalAngles < 1) return false;

            return true;
        }
    }
}

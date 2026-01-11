using OpenGL;
using System;

namespace Noise
{
    /// <summary>
    /// 단층점 데이터 - 지형의 지질학적 단층을 정의하는 보로노이 셀의 중심점
    /// 
    /// [지질학적 배경]
    /// 단층(Fault)은 지각에 힘이 가해져 암석층이 끊어지고 양쪽이 어긋난 곳입니다.
    /// 지진이 발생하는 주요 원인이며, 절벽에서 지층이 계단처럼 어긋나 보이는 현상을 만듭니다.
    /// 
    /// [보로노이 시스템에서의 역할]
    /// 각 FaultPoint는 보로노이 다이어그램의 한 셀을 대표합니다.
    /// 같은 셀 내의 모든 픽셀은 이 FaultPoint의 속성(변위, 방향)을 공유합니다.
    /// 서로 다른 셀이 만나는 경계선이 실제 단층선이 됩니다.
    /// </summary>
    public struct FaultPoint
    {
        /// <summary>
        /// Position: 단층점의 2D 위치 (0~1 정규화된 좌표)
        /// 
        /// [의미]
        /// - 지형 맵에서 이 단층 블록의 중심 위치
        /// - (0,0) = 지형 왼쪽 아래 모서리
        /// - (1,1) = 지형 오른쪽 위 모서리
        /// - (0.5, 0.5) = 지형 정중앙
        /// 
        /// [예시]
        /// - Position = (0.3, 0.7) → 지형의 왼쪽 위쪽 부근에 위치한 단층 블록
        /// - 이 위치를 중심으로 보로노이 셀이 형성됨
        /// 
        /// [실제 사용]
        /// - 보로노이 계산 시 각 픽셀이 어느 FaultPoint에 가장 가까운지 판단
        /// - 가장 가까운 FaultPoint의 속성이 해당 픽셀에 적용됨
        /// - 거리 계산: (uv - Position).Norm()
        /// </summary>
        public Vertex2f Position;

        /// <summary>
        /// Displacement: 수직 변위량 (-1.0 ~ +1.0 정규화된 값)
        /// 
        /// [지질학적 의미]
        /// 단층을 경계로 한쪽 지층이 다른 쪽에 비해 얼마나 위 또는 아래로 이동했는지를 나타냅니다.
        /// 
        /// - 양수(+): 이 블록이 인접 블록보다 위로 솟아오름 (역단층, Reverse Fault)
        ///   예) +0.8 → 이 블록의 지층이 이웃보다 높은 위치
        ///   실제 예: 히말라야 산맥 (인도판이 유라시아판 아래로 밀고 들어가 위로 솟음)
        /// 
        /// - 음수(-): 이 블록이 인접 블록보다 아래로 내려감 (정단층, Normal Fault)
        ///   예) -0.5 → 이 블록의 지층이 이웃보다 낮은 위치
        ///   실제 예: 동아프리카 열곡대 (지각이 늘어나면서 중앙부가 가라앉음)
        /// 
        /// - 0에 가까움: 변위가 거의 없음 (안정된 지괴)
        /// 
        /// [셰이더에서의 처리]
        /// 1. 정규화된 값 복원: displacement = (texValue * 2.0 - 1.0)
        ///    예) 텍스처 0.0 → -1.0, 0.5 → 0.0, 1.0 → +1.0
        /// 
        /// 2. 실제 미터 단위로 변환: 
        ///    realDisplacement = displacement * gFaultDisplacementScale
        ///    예) displacement = 0.8, scale = 100m → 실제 변위 = 80m 위로
        /// 
        /// 3. 지층 계산 시 적용:
        ///    원래 높이가 50m인 지점에서 displacement가 +0.8이고 scale이 100이면
        ///    지층 계산은 150m 높이 기준으로 수행 → 지층이 위로 어긋나 보임
        /// 
        /// [시각적 효과]
        /// - 절벽을 보면 지층 선이 단층을 경계로 계단처럼 끊어져 보임
        /// - 한쪽은 사암층, 다른 쪽은 석회암층이 같은 높이에 나타날 수 있음
        /// - 그랜드 캐년의 단층선에서 볼 수 있는 효과와 동일
        /// 
        /// [권장 값 범위]
        /// - 미묘한 변위: -0.3 ~ +0.3 (현실적, 섬세한 효과)
        /// - 중간 변위: -0.6 ~ +0.6 (눈에 띄는 효과, 기본 권장)
        /// - 극적 변위: -1.0 ~ +1.0 (과장된 효과, 게임용 드라마틱)
        /// </summary>
        public float Displacement;

        /// <summary>
        /// Direction: 단층선의 주향(走向, Strike) 방향 (0 ~ 2π 라디안)
        /// 
        /// [지질학적 의미]
        /// 단층면이 지표와 만나는 선(단층선)이 가리키는 수평 방향입니다.
        /// 나침반으로 측정하는 방위각과 같은 개념입니다.
        /// 
        /// [각도 체계]
        /// - 0 (0°): 정동쪽 (→)
        /// - π/2 (90°): 정북쪽 (↑)
        /// - π (180°): 정서쪽 (←)
        /// - 3π/2 (270°): 정남쪽 (↓)
        /// - 2π (360°): 다시 정동쪽 (한 바퀴 회전)
        /// 
        /// [실제 예시]
        /// - Direction = 0 (동서 방향 단층)
        ///   → 지층 선이 좌우로 어긋남
        ///   실제 예: 산 안드레아스 단층 (캘리포니아)
        /// 
        /// - Direction = π/2 (남북 방향 단층)
        ///   → 지층 선이 위아래로 어긋남
        ///   실제 예: 일본 열도의 주요 단층들
        /// 
        /// - Direction = π/4 (북동-남서 방향)
        ///   → 대각선 방향으로 어긋남
        /// 
        /// [현재 구현 상태]
        /// ⚠️ 주의: 현재 셰이더에서는 B 채널로 전달만 되고 실제로 사용되지 않습니다.
        /// 텍스처에 저장은 되지만, 지층 변위 계산 시에는 아직 반영되지 않습니다.
        /// 
        /// [향후 확장 가능성]
        /// 1. 방향성 변위: 수직 변위뿐 아니라 수평 변위도 적용
        ///    예) 캘리포니아 단층처럼 옆으로 밀리는 효과 (주향이동단층)
        ///    
        /// 2. 습곡 구조: 단층 방향을 따라 지층이 휘어지는 효과
        ///    strataInput에 Direction 기반 오프셋 추가
        ///    
        /// 3. 이방성 효과: 단층 방향에 따라 풍화나 침식 정도 차등 적용
        ///    
        /// 4. 수평 이동: 주향이동단층 시뮬레이션
        /// 
        /// [셰이더 확장 예시 코드]
        /// ```glsl
        /// float dirAngle = faultData.b * 2.0 * PI; // B 채널 복원
        /// vec2 faultVector = vec2(cos(dirAngle), sin(dirAngle));
        /// float horizontalDisp = dot(worldPos.xy, faultVector) * someScale;
        /// displacedPos.xy += faultVector * horizontalDisp; // 수평 변위 적용
        /// ```
        /// 
        /// [권장 값]
        /// - 랜덤 생성 시: Random.NextDouble() * Math.PI * 2.0
        /// - 특정 방향 원할 때: 원하는 각도를 라디안으로 변환
        ///   예) 정동 방향 → 0
        ///   예) 정북 방향 → Math.PI / 2
        ///   예) 북동 방향 → Math.PI / 4
        /// </summary>
        public float Direction;

        /// <summary>
        /// Width: 단층대(Fault Zone)의 폭 (0.0 ~ 1.0 정규화된 값)
        /// 
        /// [지질학적 의미]
        /// 단층은 정확한 선이 아니라 일정한 폭을 가진 "대(帶, Zone)"입니다.
        /// 단층대 내부는 오랜 지각 활동으로 암석이 파쇄되고 으스러져 있습니다.
        /// 이를 각력암(角礫岩, Breccia) 또는 단층점토(Fault Gouge)라고 부릅니다.
        /// 
        /// [실제 지형에서의 모습]
        /// - 단층 중심: 완전히 파쇄된 암석 조각들 (자갈처럼 각진 파편)
        /// - 단층대 중간: 균열이 많고 풍화된 암석
        /// - 단층대 가장자리: 온전한 암석 (약간의 균열만 존재)
        /// 
        /// [Width 값의 의미]
        /// 이 값은 보로노이 셀 경계로부터 단층대가 얼마나 넓게 퍼지는지 결정합니다.
        /// 
        /// - Width = 0.03 (3%): 매우 좁은 단층대
        ///   → 날카로운 단층선, 명확한 경계
        ///   → 실제: 소규모 단층, 최근에 형성된 단층
        ///   → 시각: 지층이 칼로 자른 듯 끊어짐
        /// 
        /// - Width = 0.08 (8%): 중간 폭 단층대
        ///   → 균형잡힌 전환
        ///   → 실제: 일반적인 단층
        ///   → 시각: 경계 부근에 파쇄된 바위 영역이 보임
        /// 
        /// - Width = 0.15 (15%): 넓은 단층대
        ///   → 부드러운 전환, 넓은 파쇄대
        ///   → 실제: 주요 단층선, 오래되고 활동이 많았던 단층
        ///   → 시각: 단층 근처가 전체적으로 어둡고 거칠어 보임
        /// 
        /// [셰이더에서의 처리]
        /// 1. 경계 거리 계산:
        ///    edgeDistance = (두 번째 가까운 셀까지 거리) - (가장 가까운 셀까지 거리)
        ///    → 셀 경계에 가까울수록 0, 중심에 가까울수록 큰 값
        /// 
        /// 2. 단층대 마스크 생성:
        ///    faultZoneMask = 1.0 - smoothstep(0.0, gFaultZoneWidth, edgeDistance)
        ///    → gFaultZoneWidth가 클수록 더 넓은 영역이 단층대로 판정됨
        ///    → 이 값은 셰이더 유니폼으로 전달 (기본값 0.05)
        /// 
        /// 3. 각력암 색상 혼합:
        ///    vec3 brecciaColor = vec3(0.45, 0.4, 0.35); // 어두운 갈색
        ///    finalColor = mix(normalColor, brecciaColor, faultZoneMask * gFaultZoneIntensity)
        ///    → 단층대 내부는 어둡고 파쇄된 바위 색상
        /// 
        /// [시각적 효과]
        /// - 단층선 = 검거나 어두운 선으로 표시됨
        /// - 단층대 = 선 주변의 어둡고 거친 영역
        /// - Width가 클수록 단층이 "두꺼운 선"처럼 보임
        /// - 절벽에서 보면 단층 경계를 따라 바위가 부서진 흔적이 보임
        /// 
        /// [실제 응용]
        /// - 주요 단층 (메인 지질 구조): Width = 0.1 ~ 0.2
        ///   예) 산안드레아스 단층, 일본 주요 단층
        ///   
        /// - 보조 단층 (작은 균열): Width = 0.03 ~ 0.08
        ///   예) 작은 절벽의 균열, 암석층 경계
        ///   
        /// - 게임 비주얼: Width를 크게 하면 단층이 눈에 잘 띔
        ///   플레이어가 쉽게 지질 구조를 인식
        ///   
        /// - 사실적 표현: Width를 작게 하면 섬세한 균열망
        ///   확대해서 보면 디테일이 드러남
        /// 
        /// [주의사항]
        /// - Width가 너무 작으면 (< 0.02): 화면에서 보이지 않을 수 있음
        ///   특히 밉맵 레벨이 낮아지면(멀리서) 사라짐
        ///   
        /// - Width가 너무 크면 (> 0.3): 셀 전체가 단층대가 되어 이상해 보임
        ///   모든 바위가 파쇄암처럼 보이는 문제
        ///   
        /// - 밉맵 레벨이 낮을 때(멀리서): Width를 자동으로 증가시키는 것이 좋음
        ///   거리에 따른 LOD 고려
        /// 
        /// [권장 값 범위]
        /// - 미세한 균열: 0.02 ~ 0.05 (세밀한 디테일)
        /// - 일반 단층: 0.05 ~ 0.10 (기본 권장) ⭐
        /// - 주요 단층: 0.10 ~ 0.20 (큰 지질 구조)
        /// - 극한 효과: 0.20 ~ 0.30 (과장된 비주얼)
        /// 
        /// [현재 구현 참고]
        /// 이 Width 값은 FaultPoint 생성 시에만 사용되고, 실제로는 각 픽셀의
        /// edgeDistance 계산 시 반영됩니다. 셰이더 유니폼 gFaultZoneWidth와
        /// 혼동하지 마세요. 이것은 보로노이 셀 생성 시의 특성값입니다.
        /// </summary>
        public float Width;

        /// <summary>
        /// 생성자: 단층점 초기화
        /// </summary>
        /// <param name="pos">정규화된 2D 위치 (0~1 범위)</param>
        /// <param name="disp">수직 변위량 (-1~+1, 양수=위로, 음수=아래로)</param>
        /// <param name="dir">단층 방향 (0~2π 라디안, 0=동쪽, π/2=북쪽)</param>
        /// <param name="width">단층대 폭 (0~1, 권장: 0.05~0.15)</param>
        public FaultPoint(Vertex2f pos, float disp, float dir, float width)
        {
            Position = pos;
            Displacement = disp;
            Direction = dir;
            Width = width;
        }

        /// <summary>
        /// 특정 방향과 변위를 가진 단층점 생성 (편의 메서드)
        /// </summary>
        public static FaultPoint CreateDirectional(
            Vertex2f position,
            float displacement,
            CardinalDirection direction,
            float width = 0.08f)
        {
            float directionRad;

            switch (direction)
            {
                case CardinalDirection.East:
                    directionRad = 0f;
                    break;
                case CardinalDirection.North:
                    directionRad = (float)(Math.PI / 2.0);
                    break;
                case CardinalDirection.West:
                    directionRad = (float)Math.PI;
                    break;
                case CardinalDirection.South:
                    directionRad = (float)(3.0 * Math.PI / 2.0);
                    break;
                case CardinalDirection.NorthEast:
                    directionRad = (float)(Math.PI / 4.0);
                    break;
                case CardinalDirection.NorthWest:
                    directionRad = (float)(3.0 * Math.PI / 4.0);
                    break;
                case CardinalDirection.SouthWest:
                    directionRad = (float)(5.0 * Math.PI / 4.0);
                    break;
                case CardinalDirection.SouthEast:
                    directionRad = (float)(7.0 * Math.PI / 4.0);
                    break;
                default:
                    directionRad = 0f;
                    break;
            }

            return new FaultPoint(position, displacement, directionRad, width);
        }
    }

    /// <summary>
    /// 방위각 열거형 (편의용)
    /// </summary>
    public enum CardinalDirection
    {
        East,       // 정동 (0°)
        North,      // 정북 (90°)
        West,       // 정서 (180°)
        South,      // 정남 (270°)
        NorthEast,  // 북동 (45°)
        NorthWest,  // 북서 (135°)
        SouthWest,  // 남서 (225°)
        SouthEast   // 남동 (315°)
    }
}

// ============================================================================
// 실전 사용 예시
// ============================================================================
/*
// 예시 1: 동서 방향의 강한 역단층 (산맥 형성 단층)
var mountainFault = new FaultPoint(
    pos: new Vertex2f(0.5f, 0.3f),      // 지형 중앙 아래쪽
    disp: 0.9f,                          // 큰 양수 = 크게 솟아오름
    dir: 0.0f,                           // 0° = 동서 방향
    width: 0.15f                         // 넓은 단층대 (주요 지질 구조)
);
// 효과: 지형 중앙에 동서로 뻗은 산맥처럼 보임

// 예시 2: 북동 방향의 소규모 정단층 (계곡 형성 단층)
var valleyFault = new FaultPoint(
    pos: new Vertex2f(0.7f, 0.6f),      // 지형 오른쪽 위
    disp: -0.4f,                         // 음수 = 가라앉음
    dir: (float)(Math.PI / 4),          // 45° = 북동 방향
    width: 0.06f                         // 좁은 단층대 (소규모 균열)
);
// 효과: 북동 방향으로 좁은 계곡이나 함몰지 형성

// 예시 3: 편의 메서드 사용
var easternFault = FaultPoint.CreateDirectional(
    position: new Vertex2f(0.2f, 0.5f),
    displacement: 0.6f,
    direction: CardinalDirection.North,
    width: 0.1f
);
// 효과: 정북 방향 단층, 중간 변위

// 예시 4: 안정된 지괴 (변위 거의 없음)
var stableBlock = new FaultPoint(
    pos: new Vertex2f(0.2f, 0.8f),      // 지형 왼쪽 위
    disp: 0.05f,                         // 거의 변위 없음
    dir: (float)(Math.PI),              // 180° = 서쪽
    width: 0.03f                         // 매우 좁은 경계
);
// 효과: 주변과 거의 비슷한 높이, 경계가 거의 안 보임
*/
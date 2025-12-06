#version 420 core

// ============================================================================
// 카메라 좌표계 및 뷰 공간 변환 시스템 설명
// ============================================================================
//
// [월드 좌표계]
// - 오른손 좌표계: X(동쪽/Right), Y(북쪽/Forward), Z(위쪽/Up)
// - Z-up 시스템: 많은 CAD, GIS, 언리얼 엔진 등에서 사용하는 방식
// - 예: 위치(100, 200, 50) = 동쪽 100m, 북쪽 200m, 고도 50m
//
// [카메라 벡터 계산 (OrbitCamera.UpdateCameraVectors)]
// 1. Yaw/Pitch 각도로부터 direction 계산
//    direction.x = Cos(yaw) * Cos(pitch)
//    direction.y = Sin(yaw) * Cos(pitch)
//    direction.z = Sin(pitch)
//
// 2. Forward 벡터 음수 변환
//    _cameraForward = -direction.Normalized
//    → 카메라가 바라보는 방향의 반대 벡터 저장
//
// 3. 기저 벡터 계산
//    _cameraRight = _cameraForward × UnitZ (외적)
//    _cameraUp = _cameraRight × _cameraForward
//
// [뷰 행렬 생성 (Matrix4x4F.CreateViewMatrix)]
// - Forward 벡터를 음수 변환 없이 그대로 사용
// - 행렬 구성:
//   | right.x    up.x    forward.x    0 |
//   | right.y    up.y    forward.y    0 |
//   | right.z    up.z    forward.z    0 |
//   | -R·pos    -U·pos   -F·pos       1 |
//
// - 표준 OpenGL은 forward에 음수를 적용하지만, 현재 시스템은
//   이미 _cameraForward가 음수이므로 그대로 사용
//
// [뷰 공간 좌표계 - ★ 중요 ★]
// 표준 OpenGL:  앞쪽 = 음수 Z (-Z가 전방)
// 현재 시스템:  앞쪽 = 양수 Z (+Z가 전방)  ← 반대!
//
// 실제 동작 확인:
// - 카메라 앞 1m:   viewPos.z = +1.0    (양수)
// - 카메라 앞 100m: viewPos.z = +100.0  (양수)
// - 카메라 뒤 100m: viewPos.z = -100.0  (음수)
//
// [깊이 버퍼 저장 방식]
// - 목적: 0~10km 범위를 [0, 1]로 선형 매핑
// - 공식: gl_FragDepth = viewPos.z / 10000.0
// - 결과:
//   0m     → 0.0   (가장 가까움, 파란색)
//   5km    → 0.5   (중간 거리, 초록색)
//   10km   → 1.0   (최대 거리, 빨간색)
//
// [왜 이 방식을 사용하는가?]
// 1. 선형 깊이 분포
//    - 표준 투영: 99.98%가 0.999 이상에 집중 (비선형)
//    - 현재 방식: 균등 분포로 모든 거리에 동일한 정밀도
//
// 2. 직관적인 값
//    - depth = 0.3 → 정확히 3km
//    - depth = 0.7 → 정확히 7km
//
// 3. R32f 부동소수점 활용
//    - 넓은 범위(-3.4e38 ~ +3.4e38) 활용
//    - 10km 원거리 렌더링에 최적화
//
// [HiZ Occlusion Culling에서의 적용]
// - 지형 깊이: viewPos.z / 10000.0 → HiZ 텍스처에 저장
// - 나무 AABB: ndc.z / 10000.0 → 지형과 동일한 스케일로 변환
// - 비교 로직: treeDepth > terrainDepth이면 가려짐 (큰 값 = 먼 거리)
//
// [주의사항]
// - 이 좌표계는 표준 OpenGL과 다르므로 외부 라이브러리 사용 시 주의
// - 셰이더에서 viewPos.z를 절댓값 없이 그대로 사용
// - 깊이 비교 시 항상 "큰 값 = 먼 거리" 규칙 적용
// ============================================================================

in vec4 viewPos;

void main()
{
	// 깊이버퍼의 최대값을 늘리기 위하여 수동으로 gl_FragDepth 설정
    // 지형의 최대 거리는 1 = 1m이므로
    // 100km = 100,000m 까지 표현하려면 float의 정밀도를 활용
    gl_FragDepth = clamp(viewPos.z / 10000.0, 0.0, 1.0);

}
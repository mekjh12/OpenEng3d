#version 430

in float lightViewDepth;  // ⭐ TES에서 전달받은 Light 뷰 깊이

// 깊이만 기록하므로 출력 불필요
// OpenGL이 자동으로 gl_FragDepth에 깊이 기록
void main()
{
    // ⭐ 깊이 출력 (0.0 ~ 1.0 범위로 정규화)
    // 사용자님의 방식 그대로 적용
    gl_FragDepth = clamp(lightViewDepth / 10000.0, 0.0, 1.0);
}
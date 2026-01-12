#version 430

in float lightViewDepth;  // ⭐ VERT에서 전달받은 Light 뷰 깊이
in vec2 vTexCoord;
in float vMaterialID;

uniform sampler2D textures[32];

// 깊이만 기록하므로 출력 불필요
// OpenGL이 자동으로 gl_FragDepth에 깊이 기록
void main()
{
    // 1. 텍스처 인덱스 검증
    int texIndex = int(vMaterialID);
    
    // 2. 텍스처 샘플링
    vec4 texColor = texture(textures[texIndex], vTexCoord);
    
    // ✅ Alpha test (투명한 부분 제거)
    if (texColor.a < 0.45) discard;

    // ⭐ 깊이 출력 (0.0 ~ 1.0 범위로 정규화)
    // 사용자님의 방식 그대로 적용
    gl_FragDepth = clamp(lightViewDepth / 10000.0, 0.0, 1.0);
}
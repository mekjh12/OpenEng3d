#version 450 core

// ============================================================
// 입력
// ============================================================

in vec2 v_TexCoord;
in float v_AO;

// ============================================================
// 출력
// ============================================================

out vec4 fragColor;

// ============================================================
// Uniform
// ============================================================

uniform sampler2D u_GrassTexture;
uniform vec3 u_SunDirection;    // 태양 방향
uniform vec3 u_GrassColorTop;   // 상단 색상 (밝은 초록)
uniform vec3 u_GrassColorBottom; // 하단 색상 (어두운 초록)

// ============================================================
// 메인
// ============================================================

void main()
{

    fragColor = vec4(0, 1, 0, 1);
    return;

    // 1. 텍스처 샘플링
    vec4 texColor = texture(u_GrassTexture, v_TexCoord);
    
    // 2. 알파 테스트 (풀 외곽 투명)
    if (texColor.a < 0.5)
        discard;
    
    // 3. 그라디언트 색상 (상단 밝게, 하단 어둡게)
    vec3 grassColor = mix(u_GrassColorBottom, u_GrassColorTop, v_TexCoord.y);
    
    // 4. 텍스처와 색상 혼합
    vec3 finalColor = texColor.rgb * grassColor;
    
    // 5. AO 적용
    finalColor *= v_AO;
    
    // 6. 간단한 라이팅 (선택사항)
    // Z-up: 법선은 대략 (0, 0, 1)
    vec3 normal = vec3(0, 0, 1);
    float NdotL = max(dot(normal, normalize(u_SunDirection)), 0.3);  // 최소 30%
    finalColor *= NdotL;
    
    // 7. 최종 출력
    fragColor = vec4(finalColor, texColor.a);
}
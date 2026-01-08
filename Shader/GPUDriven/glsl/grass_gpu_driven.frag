#version 450 core

// ============================================================
// 입력
// ============================================================
in vec2 v_TexCoord;
in float v_AO;
in vec2 v_HeightmapUV;

// ============================================================
// 출력
// ============================================================
out vec4 fragColor;

// ============================================================
// Uniforms
// ============================================================
uniform sampler2D u_GrassTexture;
uniform sampler2D u_NormalMap;        // 지형 노멀맵
uniform vec3 u_SunDirection;          // 태양 방향
uniform vec3 u_GrassColorTop;         // 상단 색상
uniform vec3 u_GrassColorBottom;      // 하단 색상

// ============================================================
// 메인
// ============================================================
void main()
{
    fragColor = vec4(0, 1, 0, 1);
    return;

    // 1. 경사도 체크 (급경사는 풀 안 자람)
    vec3 terrainNormal = texture(u_NormalMap, v_HeightmapUV).rgb;
    terrainNormal = terrainNormal * 2.0 - 1.0;  // [0,1] → [-1,1]
    
    //float slope = 1.0 - terrainNormal.z;  // 0 = 평지, 1 = 수직
    //if (slope > 0.3) discard;  // 경사 30도 이상이면 제거
    
    // 2. 텍스처 샘플링
    vec4 texColor = texture(u_GrassTexture, v_TexCoord);
    
    // 3. 알파 테스트
    if (texColor.a < 0.5) discard;
    
    // 4. 그라디언트 색상 (상단 밝게, 하단 어둡게)
    vec3 grassColor = mix(u_GrassColorBottom, u_GrassColorTop, v_TexCoord.y);
    
    // 5. 텍스처와 색상 혼합
    vec3 finalColor = texColor.rgb * grassColor;
    
    // 6. AO 적용
    finalColor *= v_AO;
    
    // 7. 간단한 라이팅
    vec3 normal = vec3(0, 0, 1);  // Z-up
    float NdotL = max(dot(normal, normalize(u_SunDirection)), 0.3);
    finalColor *= NdotL;
    
    // 8. 최종 출력
    fragColor = vec4(finalColor, texColor.a);
}
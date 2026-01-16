#version 430 core

in vec2 TexCoord;
out vec4 fragColor;

uniform sampler2D structureBuffer;
uniform int debugMode;  // 0~5: 다양한 시각화 모드
uniform float depthRange = 1000.0;  // 깊이 시각화 범위

void main()
{
    vec4 structure = texture(structureBuffer, TexCoord);
    
    // 데이터 복원
    float dzdx = structure.r / 64.0;  // 미분값 복원
    float dzdy = structure.g / 64.0;
    float h = structure.b;             // 상위 비트
    float z_minus_h = structure.a;     // 하위 비트
    float depth = h + z_minus_h;       // 전체 깊이
    
    vec3 color;
    
    if (debugMode == 0) {
        // 모드 0: 깊이 시각화 (히트맵)
        float normalizedDepth = clamp(depth / depthRange, 0.0, 1.0);
        
        // 히트맵 색상 (파랑 → 녹색 → 빨강)
        if (normalizedDepth < 0.25) {
            color = mix(vec3(0.0, 0.0, 1.0), vec3(0.0, 1.0, 1.0), normalizedDepth * 4.0);
        } else if (normalizedDepth < 0.5) {
            color = mix(vec3(0.0, 1.0, 1.0), vec3(0.0, 1.0, 0.0), (normalizedDepth - 0.25) * 4.0);
        } else if (normalizedDepth < 0.75) {
            color = mix(vec3(0.0, 1.0, 0.0), vec3(1.0, 1.0, 0.0), (normalizedDepth - 0.5) * 4.0);
        } else {
            color = mix(vec3(1.0, 1.0, 0.0), vec3(1.0, 0.0, 0.0), (normalizedDepth - 0.75) * 4.0);
        }
    }
    else if (debugMode == 1) {
        // 모드 1: dz/dx (X 방향 미분)
        // 양수 = 빨강, 음수 = 파랑
        float value = dzdx * 5.0;  // 증폭
        color = vec3(
            max(0.0, value),   // R
            0.0,               // G
            max(0.0, -value)   // B
        );
        color = clamp(color + 0.5, 0.0, 1.0);  // 중간값 0.5
    }
    else if (debugMode == 2) {
        // 모드 2: dz/dy (Y 방향 미분)
        float value = dzdy * 5.0;
        color = vec3(
            max(0.0, value),
            0.0,
            max(0.0, -value)
        );
        color = clamp(color + 0.5, 0.0, 1.0);
    }
    else if (debugMode == 3) {
        // 모드 3: 그라디언트 크기 (경사도)
        float gradient = length(vec2(dzdx, dzdy));
        gradient *= 10.0;  // 증폭
        color = vec3(gradient);
    }
    else if (debugMode == 4) {
        // 모드 4: 원본 RGBA 데이터 (정규화)
        // R: dzdx, G: dzdy, B: h, A: z-h
        color = vec3(
            structure.r,  // 빨강: dzdx (64배 증폭된 값)
            structure.g,  // 녹색: dzdy
            structure.b / depthRange  // 파랑: h (깊이 정규화)
        );
    }
    else if (debugMode == 5) {
        // 모드 5: 비트 분할 검증
        // h + (z-h) = z가 제대로 되는지 확인
        float reconstructed = h + z_minus_h;
        float normalized = reconstructed / depthRange;
        color = vec3(normalized);
    }
    else {
        // 기본: 회색
        color = vec3(0.5);
    }
    
    fragColor = vec4(color, 1.0);
}
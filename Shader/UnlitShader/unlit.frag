#version 450 core

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 📖 Unlit Shader - MRT(Multiple Render Targets) 지원
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
//
// [목적]
//   임포스터 아틀라스 생성을 위해 한 번의 렌더 패스로
//   Color, Normal, Depth 3개 텍스처를 동시에 출력
//
// [출력]
//   layout(location = 0) → ColorAttachment0  (컬러 텍스처)
//   layout(location = 1) → ColorAttachment1  (노멀 맵)
//   layout(location = 2) → ColorAttachment2  (깊이 맵)
//
// [데이터 흐름]
//   Vertex Shader → Rasterization → Fragment Shader → MRT
//
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 입력 (Vertex Shader로부터)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
in vec2 vTexCoord;      // UV 좌표 (텍스처 샘플링용)
in float vMaterialID;   // 머티리얼 인덱스 (텍스처 배열 인덱싱)
in vec3 vViewPos;       // 뷰 공간 위치 (깊이 계산용)
in vec3 vNormal;        // 뷰 공간 법선 (노멀 맵 생성용)

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 출력 (MRT - Multiple Render Targets)
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
layout(location = 0) out vec4 fragColor;   // ColorAttachment0: 최종 컬러 (RGBA)
layout(location = 1) out vec4 fragNormal;  // ColorAttachment1: 인코딩된 법선 (RGB=법선, A=1)
layout(location = 2) out vec4 fragDepth;   // ColorAttachment2: 깊이 값 (R=깊이, GBA=0)

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// Uniform 변수
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
uniform sampler2D textures[32];  // 텍스처 배열 (최대 32개 머티리얼 지원)
uniform int textureCount;        // 실제 바인딩된 텍스처 개수
uniform int enableLighting;      // 라이팅 활성화 플래그 (0=끔, 1=켬)

// UBO (Uniform Buffer Object) - 라이팅 정보
layout(std140, binding = 1) uniform LightingBlock {
    vec3 ambientColor;      // 앰비언트 라이트 색상
    vec3 lightDirection;    // 디렉셔널 라이트 방향 (뷰 공간)
    vec3 lightColor;        // 디렉셔널 라이트 색상
} lighting;

void main()
{
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 1. 머티리얼 인덱스 검증
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    int texIndex = int(vMaterialID);
    
    // 유효하지 않은 머티리얼 ID → 마젠타(디버그 색상) 출력
    if (texIndex < 0 || texIndex >= textureCount)
    {
        fragColor = vec4(1.0, 0.0, 1.0, 1.0);       // 마젠타 (에러 표시)
        fragDepth = vec4(0.0, 0.0, 0.0, 1.0);       // 깊이 0
        fragNormal = vec4(0.5, 0.5, 1.0, 1.0);      // 기본 법선 (0,0,1)
        return;
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 2. 텍스처 샘플링
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    vec4 texColor = texture(textures[texIndex], vTexCoord);
    
    // 알파 테스트: 알파값이 0.45 미만이면 프래그먼트 폐기 (투명 영역)
    if (texColor.a < 0.45) discard;
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 3. 라이팅 계산 (enableLighting == 1일 때만)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    if (enableLighting == 1) {
        // ─────────────────────────────────────────────────────────
        // 3-1. 법선 방향 조정 (양면 렌더링 대응)
        // ─────────────────────────────────────────────────────────
        vec3 normal = normalize(vNormal);
        
        // 뒷면을 렌더링할 때는 법선 방향 반전
        // gl_FrontFacing: 앞면이면 true, 뒷면이면 false
        if (!gl_FrontFacing) {
            normal = -normal;
        }
    
        // ─────────────────────────────────────────────────────────
        // 3-2. Ambient Light (주변광)
        // ─────────────────────────────────────────────────────────
        vec3 ambient = lighting.ambientColor;
    
        // ─────────────────────────────────────────────────────────
        // 3-3. Directional Light (방향광)
        // ─────────────────────────────────────────────────────────
        // Lambert 코사인 법칙: 빛과 법선의 내적으로 밝기 계산
        // -lighting.lightDirection: 빛이 향하는 방향의 반대 (빛의 소스 방향)
        float diff = max(dot(normal, -lighting.lightDirection), 0.0);
    
        // 뒷면은 약간 어둡게 (0.7배)
        if (!gl_FrontFacing) {
            diff *= 0.7;
        }
    
        vec3 diffuse = diff * lighting.lightColor;
    
        // ─────────────────────────────────────────────────────────
        // 3-4. 최종 라이팅 합성
        // ─────────────────────────────────────────────────────────
        vec3 finalLighting = ambient + diffuse;
        vec3 finalColor = texColor.rgb * finalLighting;
    
        fragColor = vec4(finalColor, texColor.a);
    } else {
        // 라이팅 비활성화: 텍스처 색상 그대로 사용 (Unlit)
        fragColor = texColor;
    }
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 4. 깊이 값 계산 및 출력 (Attachment2)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // vViewPos.z: 뷰 공간 Z좌표 (카메라가 -Z를 보므로 앞쪽 물체는 음수)
    // -vViewPos.z: 양수로 변환 (거리 개념)
    // / 100.0: 정규화 (0~100 범위를 0~1로 매핑)
    // clamp(..., 0.0, 1.0): 0~1 범위로 제한
    //
    // 결과:
    //   - 가까운 물체 (z ≈ 0)    → depth ≈ 0.0 (검은색)
    //   - 중간 거리 (z ≈ 50)     → depth ≈ 0.5 (회색)
    //   - 먼 물체 (z ≥ 100)       → depth = 1.0 (흰색)
    float depth = clamp(-vViewPos.z / 100.0, 0.0, 1.0);
    
    // R 채널에만 깊이 저장, GBA는 0 (단일 채널 사용)
    fragDepth = vec4(depth, 0.0, 0.0, 1.0);
    
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 5. 법선 인코딩 및 출력 (Attachment1)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    vec3 encodedNormal = normalize(vNormal);
    
    // 뒷면 렌더링 시 법선 반전 (양면 렌더링 대응)
    if (!gl_FrontFacing) {
        encodedNormal = -encodedNormal;
    }
    
    // 법선 인코딩: [-1, 1] 범위를 [0, 1] 범위로 변환
    // 수식: encoded = (normal + 1) / 2 = normal * 0.5 + 0.5
    // 
    // 예시:
    //   - (0, 0, 1)   → (0.5, 0.5, 1.0)  - 파란색 (위쪽 법선)
    //   - (1, 0, 0)   → (1.0, 0.5, 0.5)  - 빨간색 (오른쪽 법선)
    //   - (0, 1, 0)   → (0.5, 1.0, 0.5)  - 녹색 (앞쪽 법선)
    //   - (-1, 0, 0)  → (0.0, 0.5, 0.5)  - 사이안 (왼쪽 법선)
    fragNormal = vec4(encodedNormal * 0.5 + 0.5, 1.0);
}

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// 📝 사용 시 주의사항
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
//
// [프레임버퍼 설정]
//   - RenderTarget2D를 3개 ColorAttachment로 생성
//   - Gl.DrawBuffers([Attachment0, 1, 2]) 호출 필수
//
// [디코딩 방법]
//   - Normal: decodedNormal = encodedNormal * 2.0 - 1.0
//   - Depth:  distance = depth * 100.0 (원래 단위 복원)
//
// [성능]
//   - MRT 사용으로 3번 렌더링 → 1번 렌더링으로 최적화
//   - 대역폭: Color(4) + Normal(4) + Depth(4) = 12 bytes/pixel
//
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
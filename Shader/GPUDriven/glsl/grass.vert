#version 450 core

// ============================================================
// 입력
// ============================================================

// ✅ 구조체로 정의
struct GrassInstance
{
    vec3 position;      // 12 bytes (offset 0)
    float rotation;     // 4 bytes  (offset 12)
    float scale;        // 4 bytes  (offset 16)
    float windPhase;    // 4 bytes  (offset 20)
    float padding1;     // 4 bytes  (offset 24)
    float padding2;     // 4 bytes  (offset 28)
    // 총 32 bytes
};

layout(std430, binding = 0) buffer GrassInstances
{
    GrassInstance instances[];
};

// UBO: 카메라 행렬
layout(std140, binding = 0) uniform CameraBlock {
    mat4 view;
    mat4 proj;
    mat4 vp;
} camera;

// Uniform
uniform vec3 u_CameraRight;     // 카메라 오른쪽 벡터
uniform vec3 u_CameraUp;        // 카메라 위쪽 벡터 (Z축)
uniform float u_GrassWidth;     // 풀 너비 (기본: 0.2m)
uniform float u_GrassHeight;    // 풀 높이 (기본: 0.5m)

// ============================================================
// 출력
// ============================================================

out vec2 v_TexCoord;    // 텍스처 좌표
out float v_AO;         // Ambient Occlusion (하단 어둡게)

// ============================================================
// 메인
// ============================================================

void main()
{
    // 1. 어느 풀인지 (인스턴스 ID)
    int grassID = gl_InstanceID;
    
    // 2. 쿼드의 어느 버텍스인지 (0, 1, 2, 3)
    int vertexID = gl_VertexID % 4;
    
    // 3. SSBO에서 풀 데이터 읽기
    vec3 grassPos = instances[grassID].position;
    float rotation = instances[grassID].rotation;
    
    // 4. 쿼드 오프셋 정의 (Triangle Strip 순서)
    vec2 offsets[4] = vec2[4](
        vec2(-0.02,  0.0),   // 0: 왼쪽 하단
        vec2( 0.02,  0.0),   // 1: 오른쪽 하단
        vec2(-0.02,  1.0),   // 2: 왼쪽 상단
        vec2( 0.02,  1.0)    // 3: 오른쪽 상단
    );
    
    vec2 offset = offsets[vertexID];
    
    // 5. 회전 적용 (XY 평면에서)
    float c = cos(rotation);
    float s = sin(rotation);
    vec2 rotatedOffset = vec2(
        offset.x * c - offset.y * s,
        offset.x * s + offset.y * c
    );
    
    // 6. Y축 빌보드 (회전 유지)
    vec3 right = vec3(c, s, 0);  // 회전된 오른쪽
    vec3 up = vec3(0, 0, 1);     // Z는 항상 위
    
    // 7. 최종 월드 위치 계산
    vec3 worldPos = grassPos 
                  + right * offset.x * u_GrassWidth
                  + up * offset.y * u_GrassHeight;  // 높이는 회전 안 함
    
    // 8. 클립 공간 변환
    gl_Position = camera.vp * vec4(worldPos, 1.0);
    
    // 9. 텍스처 좌표
    v_TexCoord = vec2(offset.x + 0.5, offset.y);  // (0~1, 0~1)
    
    // 10. AO (하단 어둡게)
    v_AO = mix(0.4, 1.0, offset.y);  // 하단 40%, 상단 100%
}
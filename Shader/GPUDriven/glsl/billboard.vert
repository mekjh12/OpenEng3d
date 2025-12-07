#version 450 core
layout(location = 0) in vec3 aPosition;  // -0.5 ~ 0.5
layout(location = 1) in vec2 aTexCoord;

// SSBO (BillboardSize 제거!)
layout(std430, binding = 0) buffer TransformBuffer {
    mat4 allTransforms[];
};

layout(std430, binding = 1) buffer VisibleIndicesBuffer {
    int visibleIndices[];
};

// 유니폼
uniform mat4 vp;
uniform vec3 cameraPos;
uniform vec3 cameraRight;  // 카메라 오른쪽 벡터
uniform vec3 cameraUp;     // 카메라 위 벡터 (Z축 방향)
uniform vec2 uBillboardSize;  // 고정 크기 (width, height)

out vec2 vTexCoord;
out vec3 vWorldPos;

void main() {
    // 실제 인스턴스 인덱스
    int instanceIndex = visibleIndices[gl_InstanceID];
    
    if (instanceIndex < 0 || instanceIndex >= 90000) 
    {
        gl_Position = vec4(0, 0, 0, 0);
        return;
    }

    // 변환 행렬에서 위치 추출
    mat4 worldMatrix = allTransforms[instanceIndex];
    vec3 center = worldMatrix[3].xyz;
    
    // 고정 크기 사용! (유니폼으로 받음)
    vec2 size = uBillboardSize;
    
    // Z축이 위쪽인 좌표계에서 빌보드 생성
    // aPosition.x: 좌우 (-0.5 ~ 0.5)
    // aPosition.y: 위아래 (-0.5 ~ 0.5)
    vec3 worldPos = center 
        + cameraRight * aPosition.x * size.x    // 좌우 (width)
        + cameraUp * aPosition.y * size.y;      // 위아래 (height, Z축)
    
    vWorldPos = worldPos;
    gl_Position = vp * vec4(worldPos, 1.0);
    
    vTexCoord = aTexCoord;
}

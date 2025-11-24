#version 450 core

// 버텍스 애트리뷰트
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTexCoord;

// SSBO: 모든 변환 행렬 (90000개)
layout(std430, binding = 0) buffer TransformBuffer {
    mat4 allTransforms[];
};

// SSBO: 가시 인덱스 (컬링 후)
layout(std430, binding = 1) buffer VisibleIndicesBuffer {
    int visibleIndices[];
};

// 유니폼
uniform mat4 proj;
uniform mat4 view;

// 프래그먼트 셰이더로 전달
out vec3 vNormal;
out vec2 vTexCoord;
out vec3 vWorldPos;

void main() {
    // gl_InstanceID: Indirect Draw에서 제공 (0 ~ visibleCount-1)
    // 실제 인스턴스 인덱스 가져오기
    int instanceIndex = visibleIndices[gl_InstanceID];
    
    // 해당 인스턴스의 변환 행렬
    mat4 model = allTransforms[instanceIndex];
    
    // 월드 공간 위치
    vec4 worldPos = model * vec4(aPosition, 1.0);
    vWorldPos = worldPos.xyz;
    
    // 클립 공간 변환
    gl_Position = proj * view * worldPos;
        
    // 텍스처 좌표
    vTexCoord = aTexCoord;
}
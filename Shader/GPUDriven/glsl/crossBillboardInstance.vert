#version 450 core

layout(location = 0) in vec3 aPosition;

// AABB 구조체 정의
struct AABB{vec3 min; float pad1; vec3 max; float pad2;};

// SSBO 바인딩
layout(std430, binding = 0) buffer TransformBuffer {mat4 allTransforms[];};
layout(std430, binding = 1) buffer VisibleIndicesBuffer {int visibleIndices[];};
layout(std430, binding = 2) buffer AABBBuffer {AABB aabbs[]; };

// Uniform
uniform int batchStartOffset;
uniform uint currentBatchID;

// Geometry Shader로 전달
out VS_OUT {
    vec3 worldPos;
    vec3 color;
    float size;
    mat4 transform;
} vs_out;

void main() 
{
    // 가시 인덱스 가져오기
    uint localSlot = uint(batchStartOffset) + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];
    
    // 유효성 검사
    if (instanceIndex < 0 || instanceIndex >= 100000) 
    {
        vs_out.worldPos = vec3(0, 0, 0);
        vs_out.color = vec3(0, 0, 0);
        gl_Position = vec4(0, 0, 0, 0);
        return;
    }
    
    // Transform에서 위치 추출 (4x4 행렬의 마지막 열 = 이동)
    mat4 model = allTransforms[instanceIndex];
    vs_out.worldPos = vec3(model[3][0], model[3][1], model[3][2]);

    AABB aabb = aabbs[instanceIndex];
    vec3 extents = aabb.max - aabb.min;
    float size = max(extents.x, max(extents.y, extents.z));
    vs_out.size = size * 0.5;

    vs_out.transform = model;

    // batchID별 색상 결정
    if (currentBatchID == 0u) {
        vs_out.color = vec3(1.0, 0.0, 0.0);  // 빨강 - Batch 0
    } else if (currentBatchID == 1u) {
        vs_out.color = vec3(0.0, 1.0, 0.0);  // 초록 - Batch 1
    } else if (currentBatchID == 2u) {
        vs_out.color = vec3(0.0, 0.0, 1.0);  // 파랑 - Batch 2
    } else {
        vs_out.color = vec3(1.0, 1.0, 0.0);  // 노랑 - Batch 3+
    }
    
    // Point를 그대로 전달 (Geometry Shader에서 확장)
    gl_Position = vec4(vs_out.worldPos, 1.0);
}
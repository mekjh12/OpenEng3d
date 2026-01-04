#version 450 core
#define MAX_INSTANCES 100000

layout(location = 0) in vec3 aPosition;

struct AABB{vec3 min; float pad1; vec3 max; float pad2;};

struct InstanceModelMatrixData{mat4 modelMatrix; mat4 normalMatrix; };
layout(std430, binding = 0) buffer TransformBuffer { InstanceModelMatrixData instances[]; };
layout(std430, binding = 1) buffer VisibleIndicesBuffer {int visibleIndices[];};
layout(std430, binding = 2) buffer AABBBuffer {AABB aabbs[]; };

uniform int batchStartOffset;
uniform uint currentBatchID;

out VS_OUT {
    vec3 worldPos;
    vec3 color;
    float horizontalSize;  // ✅ XY 평면 크기
    float verticalSize;    // ✅ Z 높이
    mat4 transform;
} vs_out;

void main() 
{
    // 인스턴스 인덱스 계산
    uint localSlot = batchStartOffset + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];

    if (instanceIndex < 0 || instanceIndex >= MAX_INSTANCES) 
    {
        gl_Position = vec4(0, 0, 0, 0);
        return;
    }

    // 구조체에서 가져오기
    InstanceModelMatrixData inst = instances[instanceIndex];
    mat4 model = inst.modelMatrix;

    vs_out.worldPos = vec3(model[3][0], model[3][1], model[3][2]);
    
    AABB aabb = aabbs[instanceIndex];
    vec3 extents = aabb.max - aabb.min;
    
    // ✅ 가로/세로 분리 (아틀라스 생성과 동일한 로직)
    vs_out.horizontalSize = max(extents.x, extents.y) * 0.5;
    vs_out.verticalSize = extents.z * 0.5;
    
    vs_out.transform = model;
    
    if (currentBatchID == 0u) {
        vs_out.color = vec3(1.0, 0.0, 0.0);
    } else if (currentBatchID == 1u) {
        vs_out.color = vec3(0.0, 1.0, 0.0);
    } else if (currentBatchID == 2u) {
        vs_out.color = vec3(0.0, 0.0, 1.0);
    } else {
        vs_out.color = vec3(1.0, 1.0, 0.0);
    }
    
    gl_Position = vec4(vs_out.worldPos, 1.0);
}
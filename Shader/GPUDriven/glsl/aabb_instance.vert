#version 450 core
layout(location = 0) in vec3 aPosition;

// AABB 구조체 정의
struct AABB{vec3 min; float pad1; vec3 max; float pad2;};

layout(std430, binding = 0) buffer TransformBuffer {mat4 allTransforms[]; };
layout(std430, binding = 1) buffer VisibleIndicesBuffer {int visibleIndices[]; };
layout(std430, binding = 2) buffer AABBBuffer {AABB aabbs[]; };

uniform int batchStartOffset;
uniform uint currentBatchID;

out VS_OUT {
    vec3 aabbMin;
    vec3 aabbMax;
    uint batchID;
} vs_out;

void main() 
{
    uint localSlot = uint(batchStartOffset) + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];
    
    if (instanceIndex < 0 || instanceIndex >= 100000) 
    {
        vs_out.aabbMin = vec3(0);
        vs_out.aabbMax = vec3(0);
        vs_out.batchID = currentBatchID;
        gl_Position = vec4(0);
        return;
    }
    
    // AABB 데이터만 가져오기 (이미 월드 공간)
    AABB aabb = aabbs[instanceIndex];
    
    vs_out.aabbMin = aabb.min;
    vs_out.aabbMax = aabb.max;
    vs_out.batchID = currentBatchID;    
    gl_Position = vec4(0);
}
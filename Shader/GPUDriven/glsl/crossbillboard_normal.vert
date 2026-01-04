#version 450 core
layout(location = 0) in vec3 aPosition;

// AABB 구조체
struct AABB {vec3 min; float pad1; vec3 max; float pad2;};

// SSBO 바인딩
layout(std430, binding = 0) buffer TransformBuffer {mat4 allTransforms[];};
layout(std430, binding = 1) buffer VisibleIndicesBuffer {int visibleIndices[];};
layout(std430, binding = 2) buffer AABBBuffer {AABB aabbs[];};

// Uniform
uniform int batchStartOffset;
uniform uint currentBatchID;

// Geometry Shader로 전달
out VS_OUT {
    vec3 worldPos;
    mat4 transform;
    float size;
} vs_out;

void main() 
{
    uint localSlot = uint(batchStartOffset) + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];
    
    if (instanceIndex < 0 || instanceIndex >= 100000) 
    {
        vs_out.worldPos = vec3(0);
        vs_out.size = 0.0;
        gl_Position = vec4(0);
        return;
    }
    
    mat4 model = allTransforms[instanceIndex];
    vs_out.worldPos = vec3(model[3][0], model[3][1], model[3][2]);
    vs_out.transform = model;
    
    // AABB에서 크기 계산
    AABB aabb = aabbs[instanceIndex];
    vec3 extents = aabb.max - aabb.min;
    vs_out.size = max(extents.x, max(extents.y, extents.z)) * 0.5;
    
    gl_Position = vec4(vs_out.worldPos, 1.0);
}
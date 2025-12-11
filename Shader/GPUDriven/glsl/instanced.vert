#version 450 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;

layout(std430, binding = 0) buffer TransformBuffer { mat4 allTransforms[]; };
layout(std430, binding = 1) buffer VisibleIndicesBuffer { int visibleIndices[]; };
layout(std430, binding = 9) readonly buffer BatchIDs { uint batchIDs[]; };

uniform mat4 vp;
uniform int batchStartOffset;
uniform int currentBatchID;  // ⭐ 추가: 현재 렌더링 중인 배치 ID

out vec3 vNormal;
out vec2 vTexCoord;
out vec3 vWorldPos;

void main() 
{
    uint localSlot = batchStartOffset + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];
    
    if (instanceIndex < 0 || instanceIndex >= 100000) 
    {
        gl_Position = vec4(0, 0, 0, 0);
        return;
    }
    
    // ⭐ 배치 ID 검증
    uint instanceBatchID = batchIDs[instanceIndex];
    if (instanceBatchID != currentBatchID)
    {
        // 다른 배치의 인스턴스면 무시
        gl_Position = vec4(0, 0, 0, 0);
        return;
    }
    
    mat4 model = allTransforms[instanceIndex];
    vec4 worldPos = model * vec4(aPosition, 1.0);
    vWorldPos = worldPos.xyz;
    gl_Position = vp * worldPos;
    
    mat3 normalMatrix = mat3(transpose(inverse(model)));
    vNormal = normalize(normalMatrix * aNormal);
    vTexCoord = aTexCoord;
}
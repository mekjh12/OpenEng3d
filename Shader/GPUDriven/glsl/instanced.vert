#version 450 core

#define MAX_INSTANCES 100000

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in float materialID;

layout(std430, binding = 0) buffer TransformBuffer { mat4 allTransforms[]; };
layout(std430, binding = 1) buffer VisibleIndicesBuffer { int visibleIndices[]; };

uniform mat4 vp;
uniform int batchStartOffset;

out vec3 vNormal;
out vec2 vTexCoord;
out vec3 vWorldPos;
out float vMaterialID;

void main() 
{
    uint localSlot = batchStartOffset + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];
    
    if (instanceIndex < 0 || instanceIndex >= MAX_INSTANCES) 
    {
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
    vMaterialID = materialID;
}
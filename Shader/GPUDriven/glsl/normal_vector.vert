#version 450 core
#define MAX_INSTANCES 100000

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in float materialID;

layout(std430, binding = 0) buffer TransformBuffer { mat4 allTransforms[]; };
layout(std430, binding = 1) buffer VisibleIndicesBuffer { int visibleIndices[]; };

uniform mat4 vp;
uniform mat4 view;
uniform int batchStartOffset;

out VS_OUT {
    vec3 worldPos;
    vec3 worldNormal;
} vs_out;

void main() 
{
    uint localSlot = batchStartOffset + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];
    
    if (instanceIndex < 0 || instanceIndex >= MAX_INSTANCES) 
    {
        gl_Position = vec4(0, 0, 0, 0);
        vs_out.worldPos = vec3(0);
        vs_out.worldNormal = vec3(0);
        return;
    }
    
    mat4 model = allTransforms[instanceIndex];
    vec4 worldPos = model * vec4(aPosition, 1.0);
    
    // 법선 변환 (normal matrix 사용)
    mat3 normalMatrix = mat3(transpose(inverse(model)));
    vec3 worldNormal = normalize(normalMatrix * aNormal);
    
    // Geometry Shader로 전달
    vs_out.worldPos = worldPos.xyz;
    vs_out.worldNormal = worldNormal;
    
    gl_Position = worldPos;  // Geometry Shader에서 변환
}
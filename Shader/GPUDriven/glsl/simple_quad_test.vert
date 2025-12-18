#version 450 core

layout(location = 0) in vec3 aPosition;

layout(std430, binding = 0) buffer TransformBuffer {mat4 allTransforms[]; };
layout(std430, binding = 1) buffer VisibleIndicesBuffer {int visibleIndices[]; };

uniform int batchStartOffset;
uniform uint currentBatchID;

out VS_OUT {
    vec3 worldPos;
    vec3 color;
} vs_out;

void main() 
{
    // 가시 인덱스 가져오기
    uint localSlot = uint(batchStartOffset) + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];
    
    if (instanceIndex < 0 || instanceIndex >= 100000) 
    {
        vs_out.worldPos = vec3(0, 0, 0);
        vs_out.color = vec3(0, 0, 0);
        gl_Position = vec4(0, 0, 0, 0);
        return;
    }
    
    // Transform에서 위치 추출
    mat4 model = allTransforms[instanceIndex];
    vs_out.worldPos = vec3(model[3][0], model[3][1], model[3][2]);
    
    // batchID별 색상 결정
    if (currentBatchID == 0u) {
        vs_out.color = vec3(1.0, 0.0, 0.0);  // 빨강
    } else if (currentBatchID == 1u) {
        vs_out.color = vec3(0.0, 1.0, 0.0);  // 초록
    } else if (currentBatchID == 2u) {
        vs_out.color = vec3(0.0, 0.0, 1.0);  // 파랑
    } else {
        vs_out.color = vec3(1.0, 1.0, 0.0);  // 노랑
    }
    
    gl_Position = vec4(vs_out.worldPos, 1.0);
}
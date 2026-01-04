#version 450 core
layout(location = 0) in vec3 aPosition;

struct InstanceModelMatrixData{mat4 modelMatrix; mat4 normalMatrix; };
layout(std430, binding = 0) buffer TransformBuffer { InstanceModelMatrixData instances[]; };
layout(std430, binding = 1) buffer VisibleIndicesBuffer {int visibleIndices[];};

uniform int batchStartOffset;
uniform mat4 model;			// 모델 변환 행렬

out VS_OUT 
{
    vec3 worldPosition;
    mat4 modelMatrix;
} vs_out;

void main() 
{
    // 가시 인덱스 가져오기
    uint localSlot = uint(batchStartOffset) + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];
    
    if (instanceIndex < 0 || instanceIndex >= 100000) 
    {
        vs_out.worldPosition = vec3(0, 0, 0);
        vs_out.modelMatrix = mat4(0.0);
        gl_Position = vec4(0, 0, 0, 0);
        return;
    }
    
    // 구조체에서 가져오기
    InstanceModelMatrixData inst = instances[instanceIndex];
    mat4 model = inst.modelMatrix;
    
    // 월드 위치 추출
    vs_out.worldPosition = vec3(model[3][0], model[3][1], model[3][2]);
    vs_out.modelMatrix = model;
    
    gl_Position = vec4(vs_out.worldPosition, 1.0);
}
#version 450 core
layout(location = 0) in vec3 aPosition;

struct InstanceModelMatrixData{mat4 modelMatrix; mat4 normalMatrix; }; // 128 bytes
struct BatchInfo {float lodDistance; uint startIndex; uint count; uint padding;};

layout(std430, binding = 0) buffer TransformBuffer { InstanceModelMatrixData instances[]; };
layout(std430, binding = 1) buffer VisibleIndicesBuffer {int visibleIndices[];};
layout(std430, binding = 2) buffer BatchIDBuffer {uint batchIDs[];};
layout(std430, binding = 3) readonly buffer BatchInfoBuffer { BatchInfo batches[];}; // 배치 메타데이터
uniform int batchStartOffset;

out VS_OUT 
{
    vec3 worldPosition;
    mat4 modelMatrix;
    mat3 normalMatrix;
    int baseInfoIndex;
} vs_out;

void main() 
{
    // 배치 메타데이터 로드 (0번 인덱스는 전역 정보)
    uint MAX_INSTANCES = batches[0].startIndex;
    uint MAX_BATCHES = batches[0].count;

    // 가시 인덱스 가져오기
    uint localSlot = uint(batchStartOffset) + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];
    
    if (instanceIndex < 0 || instanceIndex >= MAX_INSTANCES) 
    {
        vs_out.worldPosition = vec3(0, 0, 0);
        vs_out.modelMatrix = mat4(0.0);
        gl_Position = vec4(0, 0, 0, 0);
        return;
    }
    
    // 구조체에서 가져오기
    InstanceModelMatrixData inst = instances[instanceIndex];
    mat4 model = inst.modelMatrix;
    
    // 핵심: 인스턴스의 BatchID를 SSBO에서 읽기
    uint batchID = batchIDs[instanceIndex];

    // 월드 위치 추출
    vs_out.worldPosition = vec3(model[3][0], model[3][1], model[3][2]);
    vs_out.modelMatrix = model;
    vs_out.normalMatrix = mat3(inst.normalMatrix);
    vs_out.baseInfoIndex = int(batchID);

    gl_Position = vec4(vs_out.worldPosition, 1.0);
}
#version 450 core
#define MAX_INSTANCES 100000

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in float materialID;

struct InstanceModelMatrixData{mat4 modelMatrix; mat4 normalMatrix; };
layout(std430, binding = 0) buffer TransformBuffer { InstanceModelMatrixData instances[]; };
layout(std430, binding = 1) buffer VisibleIndicesBuffer { int visibleIndices[]; };

uniform mat4 lightView;
uniform mat4 lightProj;
uniform int batchStartOffset;

out float lightViewDepth;
out vec2 vTexCoord;
out float vMaterialID;

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

    vec4 worldPos = model * vec4(aPosition, 1.0);

    // 버텍스 셰이더 출력 설정
    vec4 lightSpacePos = lightView * worldPos;
    lightViewDepth = -lightSpacePos.z;
    vMaterialID = materialID;
    vTexCoord = aTexCoord;

    // 출력
    gl_Position = lightProj * lightSpacePos;
}
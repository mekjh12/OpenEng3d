#version 450 core
#define MAX_INSTANCES 100000

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in float materialID;

struct InstanceModelMatrixData{mat4 modelMatrix; mat4 normalMatrix; };

// SSBO (Shader Storage Buffer)
layout(std430, binding = 0) buffer TransformBuffer { InstanceModelMatrixData instances[]; };
layout(std430, binding = 1) buffer VisibleIndicesBuffer { int visibleIndices[]; };

// UBO (Uniform Buffer Object)
layout(std140, binding = 0) uniform CameraBlock {mat4 view; mat4 proj; mat4 vp; vec4 cameraPos;} camera;

uniform int batchStartOffset;
out vec3 vNormal;
out vec2 vTexCoord;
out vec3 vWorldPos;
out float vMaterialID;
out vec3 vViewPos;

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
    mat3 normalMat = mat3(inst.normalMatrix);
    vec4 worldPos = model * vec4(aPosition, 1.0);
    gl_Position = camera.vp * worldPos;

    // 버텍스 셰이더 출력 설정
    vWorldPos = worldPos.xyz;
    vViewPos = (camera.view * worldPos).xyz;
    vNormal = normalize(normalMat * aNormal);
    vTexCoord = aTexCoord;
    vMaterialID = materialID;
}
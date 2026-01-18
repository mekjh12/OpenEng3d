#version 450 core

// GPU 인스턴싱 깊이 전용 Vertex Shader (Temporal Z-PrePass)
// 이전 프레임의 가시 객체들을 HiZ 버퍼에 렌더링

#define MAX_INSTANCES 100000

// Vertex Attributes
layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec2 aTexCoord;
layout(location = 2) in vec3 aNormal;
layout(location = 3) in float materialID;

// Instance Transform 구조체
struct InstanceModelMatrixData {
    mat4 modelMatrix;
    mat4 normalMatrix;
};

// SSBO: 모든 인스턴스의 Transform
layout(std430, binding = 0) buffer TransformBuffer { 
    InstanceModelMatrixData instances[]; 
};

// SSBO: 가시 인스턴스 인덱스 배열 (GPU 컬링 결과)
layout(std430, binding = 1) buffer VisibleIndicesBuffer { 
    int visibleIndices[]; 
};

// UBO: 카메라 행렬
layout(std140, binding = 0) uniform CameraBlock {
    mat4 view;
    mat4 proj;
    mat4 vp;
} camera;

// 현재 배치의 시작 오프셋
uniform int batchStartOffset;

// Fragment Shader로 전달
out float viewDepth;    // 뷰 공간 깊이
out vec2 vTexCoord;     // 알파 테스트용
out float vMaterialID;  // 텍스처 인덱스

void main()
{
    // 가시 인스턴스 인덱스 계산
    uint localSlot = batchStartOffset + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];
    
    // 유효성 검사
    if (instanceIndex < 0 || instanceIndex >= MAX_INSTANCES) 
    {
        gl_Position = vec4(0, 0, 0, 0);
        return;
    }
    
    // Transform 로드
    InstanceModelMatrixData inst = instances[instanceIndex];
    mat4 model = inst.modelMatrix;
    
    // 좌표 변환
    vec4 worldPos = model * vec4(aPosition, 1.0);
    gl_Position = camera.vp * worldPos;
    
    // 뷰 공간 깊이 계산 (앞 = +Z)
    vec4 viewPos = camera.view * worldPos;
    viewDepth = viewPos.z;
    
    // Fragment Shader로 전달
    vTexCoord = aTexCoord;
    vMaterialID = materialID;
}
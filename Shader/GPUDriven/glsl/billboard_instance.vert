#version 450 core

layout(location = 0) in vec3 aPosition;

struct InstanceModelMatrixData {
    mat4 modelMatrix; 
    mat4 normalMatrix; 
}; // 128 bytes

// 인스턴스 데이터 버퍼
layout(std430, binding = 0) buffer TransformBuffer { 
    InstanceModelMatrixData instances[]; 
};
layout(std430, binding = 1) buffer VisibleIndicesBuffer { 
    int visibleIndices[]; 
};

// UBO (Uniform Buffer Object)
layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp; 
    vec4 cameraPos;
} camera;

uniform int batchStartOffset;

out VS_OUT 
{
    vec3 worldPosition;  // 빌보드 중심 위치
    float scale;         // 빌보드 크기
    vec3 cameraPos;      // 카메라 위치 전달
} vs_out;

void main() 
{
    uint localSlot = uint(batchStartOffset) + uint(gl_InstanceID);
    int instanceIndex = visibleIndices[localSlot];
    
    if (instanceIndex < 0 || instanceIndex >= 100000) 
    {
        vs_out.worldPosition = vec3(0);
        vs_out.scale = 0.0;
        vs_out.cameraPos = camera.cameraPos.xyz;
        gl_Position = vec4(0);
        return;
    }
    
    // modelMatrix 추출
    InstanceModelMatrixData inst = instances[instanceIndex];
    mat4 transform = inst.modelMatrix;
    
    // 월드 위치 (4번째 열)
    vs_out.worldPosition = transform[3].xyz;
    
    // 스케일 추출 (첫 번째 열의 길이)
    vs_out.scale = length(transform[0].xyz);
    
    // 카메라 위치 전달
    vs_out.cameraPos = camera.cameraPos.xyz;
    
    // Geometry Shader에서 처리하므로 위치는 0
    gl_Position = vec4(0);
}
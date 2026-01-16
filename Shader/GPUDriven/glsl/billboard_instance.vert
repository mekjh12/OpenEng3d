#version 450 core

layout(location = 0) in vec3 aPosition;

// 인스턴스 데이터 버퍼
layout(std430, binding = 0) buffer TransformBuffer 
{
    mat4 allTransforms[];
};

layout(std430, binding = 1) buffer VisibleIndicesBuffer 
{
    int visibleIndices[];
};

uniform int batchStartOffset;
uniform vec3 cameraPosition;

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
        vs_out.cameraPos = cameraPosition;
        gl_Position = vec4(0);
        return;
    }
    
    // 변환 행렬에서 위치와 스케일 추출
    mat4 transform = allTransforms[instanceIndex];
    
    // 월드 위치 (4번째 열)
    vs_out.worldPosition = vec3(transform[3][0], transform[3][1], transform[3][2]);
    
    // 스케일 추출 (첫 번째 열의 길이)
    vs_out.scale = length(vec3(transform[0][0], transform[1][0], transform[2][0]));
    
    // 카메라 위치 전달
    vs_out.cameraPos = cameraPosition;
    
    // Geometry Shader에서 처리하므로 위치는 0
    gl_Position = vec4(0);
}
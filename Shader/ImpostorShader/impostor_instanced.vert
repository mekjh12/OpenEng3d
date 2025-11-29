
#version 430

// 버텍스 입력: 정점의 로컬 좌표 (포인트 하나)
in vec3 position;

// SSBO로 변환 행렬과 visible 인덱스 받기
layout(std430, binding = 0) buffer TransformBuffer {
    mat4 transforms[];
};

layout(std430, binding = 1) buffer VisibleIndicesBuffer {
    int visibleIndices[];
};

// 지오메트리 셰이더로 전달할 데이터
out VS_OUT {
    vec3 worldPosition;     // 인스턴스의 월드 위치
    mat4 modelMatrix;       // 인스턴스의 모델 행렬
} vs_out;

void main()
{
    // gl_InstanceID를 사용해 visible 인덱스 배열에서 실제 인스턴스 인덱스 가져오기
    int instanceIndex = visibleIndices[gl_InstanceID];
    mat4 transform = transforms[instanceIndex];
    
    // 변환 행렬에서 월드 위치 추출 (행렬의 4번째 열)
    vs_out.worldPosition = vec3(transform[3][0], transform[3][1], transform[3][2]);
    vs_out.modelMatrix = transform;
    
    // 포인트 위치 그대로 전달
    gl_Position = vec4(position, 1.0);
}
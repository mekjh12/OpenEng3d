#version 430

// 버텍스 쉐이더 입력: 정점의 로컬 좌표 (모델 공간)
in vec3 position;			// 물체의 위치점 한 개만 들어온다.

uniform mat4 model;			// 모델 변환 행렬

// 지오메트리 셰이더로 전달할 데이터
out VS_OUT {
    vec3 worldPosition;     // 인스턴스의 월드 위치
    mat4 modelMatrix;       // 인스턴스의 모델 행렬
} vs_out;

void main()
{
   // 변환 행렬에서 월드 위치 추출 (행렬의 4번째 열)
    vs_out.worldPosition = vec3(model[3][0], model[3][1], model[3][2]);
    vs_out.modelMatrix = model;
        
    // 포인트 위치 그대로 전달
    gl_Position = vec4(position, 1.0);
}
//-----------------------------------------------------------------------------
// 테셀레이션 제어 셰이더 (Tessellation Control Shader)
// 거리 기반 적응형 테셀레이션을 구현
//-----------------------------------------------------------------------------
#version 430

// 패치당 4개의 제어점 출력 설정

layout(vertices = 4) out;

// 버텍스 셰이더로부터의 입력
in vec2 Tex1[];           // 입력 텍스처 좌표 배열

// 테셀레이션 평가 셰이더로의 출력
out vec2 Tex2[];          // 출력 텍스처 좌표 배열
out vec4 ViewSpacePos[];  // 뷰 공간 위치 배열

// 변환 관련 uniform 변수들
uniform mat4 model;                // 모델 행렬
uniform mat4 view;                 // 뷰 행렬

void main()
{
    // 제어점 위치와 텍스처 좌표를 출력으로 전달
    gl_out[gl_InvocationID].gl_Position = gl_in[gl_InvocationID].gl_Position;
    Tex2[gl_InvocationID] = Tex1[gl_InvocationID];

    // 테셀레이션 제어 상수
    const int SIMPLE_UNIFORN_INNER_LEVEL = 8;
    const int SIMPLE_UNIFORN_OUTER_LEVEL = 20 * 8; // 20: 청크의 가로 갯수, 8: chunk shader의 min_level

    // 테셀레이션 레벨 설정 (chunk tcs와 동일한 값을 처리한다.)
    gl_TessLevelOuter[0] = SIMPLE_UNIFORN_OUTER_LEVEL;  // 좌측 엣지
    gl_TessLevelOuter[1] = SIMPLE_UNIFORN_OUTER_LEVEL;  // 하단 엣지
    gl_TessLevelOuter[2] = SIMPLE_UNIFORN_OUTER_LEVEL;  // 우측 엣지
    gl_TessLevelOuter[3] = SIMPLE_UNIFORN_OUTER_LEVEL;  // 상단 엣지

    // 단계 6: 내부 테셀레이션 레벨 설정
    // 대응되는 외곽 엣지들 중 높은 값 사용
    gl_TessLevelInner[0] = SIMPLE_UNIFORN_INNER_LEVEL;
    gl_TessLevelInner[1] = SIMPLE_UNIFORN_INNER_LEVEL;
}
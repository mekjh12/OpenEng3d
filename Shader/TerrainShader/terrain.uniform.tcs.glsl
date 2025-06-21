//----------------------------------------------------------------------------
// 테셀레이션 제어 셰이더 (Tessellation Control Shader)
// 거리 기반 적응형 테셀레이션을 구현
//-----------------------------------------------------------------------------
#version 430

// 패치당 4개의 제어점 출력 설정
layout(vertices = 4) out;

in vec2 Tex1[];           // 입력 텍스처 좌표 배열
out vec2 Tex2[];          // 출력 텍스처 좌표 배열

void main()
{
    // 제어점 위치와 텍스처 좌표를 출력으로 전달
    gl_out[gl_InvocationID].gl_Position = gl_in[gl_InvocationID].gl_Position;
    Tex2[gl_InvocationID] = Tex1[gl_InvocationID];

    gl_TessLevelOuter[0] = 32;
    gl_TessLevelOuter[1] = 32;
    gl_TessLevelOuter[2] = 32;
    gl_TessLevelOuter[3] = 32;

    gl_TessLevelInner[0] = 32;
    gl_TessLevelInner[1] = 32;
}
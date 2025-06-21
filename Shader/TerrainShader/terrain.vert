//-----------------------------------------------------------------------------
// 2D 렌더링을 위한 기본 버텍스 셰이더
//-----------------------------------------------------------------------------

#version 430

// 버텍스 입력 속성
in vec3 position;     // 정점 위치 (x, y, z)
in vec2 texCoord;     // 텍스처 좌표 (u, v)
in vec3 color;        // 정점 색상 (r, g, b) - 현재 미사용

// 프래그먼트 셰이더로 전달할 출력
out vec2 Tex1;        // 텍스처 좌표

void main()
{
   // NDC(Normalized Device Coordinates) 공간으로 정점 변환
   gl_Position = vec4(position, 1.0);
   
   // 텍스처 좌표를 프래그먼트 셰이더로 전달
   Tex1 = texCoord;
}
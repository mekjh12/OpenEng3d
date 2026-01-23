#version 430

out vec4 fragColor;

uniform vec4 u_color; // 박스 색상

void main()
{
    // 조명 계산 없이 설정된 색상 그대로 출력
    fragColor = u_color;
}
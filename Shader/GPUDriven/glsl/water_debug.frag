#version 430 core

in vec2 v_TexCoord;
out vec4 fragColor;

uniform sampler2D u_WaterBuffer;

void main()
{
    // waterBuffer에서 데이터 읽기
    vec2 waterData = texture(u_WaterBuffer, v_TexCoord).rg;
    
    // r 채널(heightmap 값)을 그레이스케일로 출력
    float value = waterData.r;
    
    // 텍스처 좌표를 색상으로 표시 (좌하단=검정, 우상단=노랑)
    fragColor = vec4(v_TexCoord.x, v_TexCoord.y, 0.0, 1.0);
}
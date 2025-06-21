#version 420 core

uniform sampler2D gColorMap;
uniform vec3 gCameraPos;

// 입력 변수 (Geometry Shader에서 전달)
in vec2 TexCoord;
in vec4 FragPos;

// 출력 변수
out vec4 FragColor;

void main()                                                                         
{                                                                                   
    vec4 textureColor = texture(gColorMap, TexCoord);
    if (textureColor.a < 0.5) discard; // 투명도 처리
    FragColor = textureColor;
}

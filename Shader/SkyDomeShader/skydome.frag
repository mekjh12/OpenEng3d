#version 430 core

// 버텍스 셰이더에서 전달된 입력 변수
in vec2 fragTexCoord;

// 유니폼 변수
uniform sampler2D skyTexture; // 하늘 텍스처

// 출력 색상
out vec4 fragColor;

void main()
{
    // 텍스처에서 색상 샘플링
    fragColor = texture(skyTexture, fragTexCoord);
}
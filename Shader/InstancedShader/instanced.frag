#version 430 core

// 입력
in vec2 pass_texcoord;

// 유니폼
uniform sampler2D modelTexture;

// 출력
out vec4 out_color;

void main()
{
    vec4 textureColor4 = texture(modelTexture, pass_texcoord);
    if (textureColor4.a < 0.05f) discard;
    out_color = textureColor4;
}
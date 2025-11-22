#version 430 core

in vec4 gViewPos;
in vec2 pass_texcoord;  // ✅ 텍스처 좌표 받기

uniform sampler2D modelTexture;  // ✅ 텍스처 유니폼 추가

void main()
{
    // ✅ 알파 테스트
    vec4 textureColor = texture(modelTexture, pass_texcoord);
    if (textureColor.a < 0.05) 
        discard;
    
    gl_FragDepth = gViewPos.z / 10000.0f;
}
#version 420 core

uniform sampler2D gColorMap;
uniform vec3 gCameraPos;
uniform vec3 fogColor;
uniform float fogDensity;
uniform vec4 fogPlane;

in vec2 TexCoord;
in vec4 FragPos;

out vec4 fragColor;

void main()                                                                         
{                                                                                   
    vec4 textureColor4 = texture2D(gColorMap, TexCoord);
    
    // ✅ 알파 테스트
    if (textureColor4.a < 0.1f) discard;
    
    // ✅ 텍스처 색상 그대로 출력 (return 제거)
    fragColor = textureColor4;
    
    // ✅ 또는 약간의 색조 추가
    // fragColor = vec4(textureColor4.rgb * fogColor, textureColor4.a);
}
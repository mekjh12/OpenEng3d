#version 430 core

// 버텍스 셰이더에서 받은 값
in vec3 vNormal;
in vec2 vTexCoord;
in vec3 vWorldPos;

// 출력
out vec4 fragColor;

// 유니폼
uniform sampler2D uTexture;

void main() 
{   
    // 텍스처 샘플링
    vec4 texColor = texture(uTexture, vTexCoord);
 
    // 출력
    fragColor = vec4(1);
    
    // 알파 테스트 (필요 시)
    if (fragColor.a < 0.1) discard;

}


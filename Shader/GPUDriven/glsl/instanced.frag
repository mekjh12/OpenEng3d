#version 430 core

// 버텍스 셰이더에서 받은 값
in vec3 vNormal;
in vec2 vTexCoord;
in vec3 vWorldPos;
in float vMaterialID;

// 출력
out vec4 fragColor;

// ✅ Uniform sampler2D 배열 (최대 32개)
uniform sampler2D textures[32];
uniform int textureCount;  // 실제 텍스처 개수

void main() 
{   
    int texIndex = int(vMaterialID);
    
    // 범위 체크
    if (texIndex < 0 || texIndex >= textureCount)
    {
        fragColor = vec4(1.0, 0.0, 1.0, 1.0);  // 마젠타 = 에러
        return;
    }
    
    vec4 texColor = texture(textures[texIndex], vTexCoord);
    
    if (texColor.a < 0.05) discard;
    fragColor = texColor;
}


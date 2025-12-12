#version 450 core

in vec2 vTexCoord;
in float vMaterialID;

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
#version 450 core

// ✅ Vertex Shader에서 받음
in float viewDepth;
in vec2 vTexCoord;
in float vMaterialID;

uniform sampler2D textures[32];
uniform int textureCount;  // 실제 텍스처 개수

void main()
{
    int texIndex = int(vMaterialID);
    
    // 범위 체크
    if (texIndex < 0 || texIndex >= textureCount)
    {
        return;
    }
    
    vec4 texColor = texture(textures[texIndex], vTexCoord);
    
    if (texColor.a < 0.01) discard;

    // 지형과 동일한 방식으로 선형 깊이 출력
    gl_FragDepth = clamp(viewDepth / 10000.0, 0.0, 1.0);
}
#version 450 core

in float viewDepth;
in vec2 vTexCoord;
in float vMaterialID;

uniform sampler2D textures[32];
uniform int textureCount;

void main()
{
    int texIndex = int(vMaterialID);
    
    // ✅ 범위 체크 - discard로 변경
    if (texIndex < 0 || texIndex >= textureCount)
    {
        discard;  // 또는 gl_FragDepth만 설정하고 계속 진행
    }
    
    vec4 texColor = texture(textures[texIndex], vTexCoord);
    
    if (texColor.a < 0.01) discard;
    
    // 깊이 출력
    gl_FragDepth = clamp(viewDepth / 10000.0, 0.0, 1.0);
}
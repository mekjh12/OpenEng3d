#version 450 core

in vec2 fTexCoord;
in flat int fPlaneIndex;
in vec3 vColor;

out vec4 FragColor;

uniform sampler2D atlasTexture;
uniform bool useTexture;  // 텍스처 사용 여부

void main() 
{
    if (useTexture)
    {
        // ✅ 텍스처 샘플링
        vec4 texColor = texture(atlasTexture, fTexCoord);
        
        // 알파 테스트
        if (texColor.a < 0.1)
            discard;
        
        FragColor = texColor;
    }
    else
    {
        // 디버그 모드: batchID 색상 표시
        FragColor = vec4(vColor, 1.0);
    }
}
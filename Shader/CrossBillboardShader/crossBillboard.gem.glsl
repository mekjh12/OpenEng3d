#version 450 core

layout(points) in;
layout(triangle_strip, max_vertices = 12) out;  // 4 quads * 4 vertices

// Vertex Shader 출력
in VS_OUT {
    vec3 worldPosition;
    float scale;
} gs_in[];

// Fragment Shader로 전달
out vec2 fTexCoord;
out flat int fPlaneIndex;

// Uniform
uniform mat4 vp;
uniform float objectWidth;
uniform float objectHeight;
uniform float horizontalTopRatio;
uniform float horizontalBottomRatio;

// ✅ Atlas 영역을 직접 계산하는 함수들
vec2 GetAtlasOffset(int planeIndex)
{
    // 0~3: 수직 평면 (상단 줄, 각 0.25 x 0.5)
    if (planeIndex < 3)
    {
        return vec2(planeIndex * 0.25, 0.0);
    }
}

vec2 GetAtlasSize(int planeIndex)
{
    return vec2(0.25, 1.0f);
}

// Quad 정점 생성 헬퍼
void EmitQuad(vec3 center, vec3 right, vec3 up, float width, float height, int planeIndex)
{
    vec3 halfRight = right * width * 0.5;
    vec3 halfUp = up * height * 0.5;
    
    // ✅ 계산된 UV 영역 사용
    vec2 uvOffset = GetAtlasOffset(planeIndex);
    vec2 uvSize = GetAtlasSize(planeIndex);
    
    // 좌하
    gl_Position = vp * vec4(center - halfRight - halfUp, 1.0);
    fTexCoord = uvOffset;
    fPlaneIndex = planeIndex;
    EmitVertex();
    
    // 우하
    gl_Position = vp * vec4(center + halfRight - halfUp, 1.0);
    fTexCoord = uvOffset + vec2(uvSize.x, 0.0);
    fPlaneIndex = planeIndex;
    EmitVertex();
    
    // 좌상
    gl_Position = vp * vec4(center - halfRight + halfUp, 1.0);
    fTexCoord = uvOffset + vec2(0.0, uvSize.y);
    fPlaneIndex = planeIndex;
    EmitVertex();
    
    // 우상
    gl_Position = vp * vec4(center + halfRight + halfUp, 1.0);
    fTexCoord = uvOffset + uvSize;
    fPlaneIndex = planeIndex;
    EmitVertex();
    
    EndPrimitive();
}

void main()
{
    vec3 center = gs_in[0].worldPosition;  // ✅ 이제 바닥 중심
    float scale = gs_in[0].scale;
    
    float width = objectWidth * scale;
    float height = objectHeight * scale;
    
    // === 수직 평면 3개 ===
    float angles[3] = float[3](0.0, 60.0, 120.0);
    
    for (int i = 0; i < 3; i++)
    {
        float angleRad = radians(angles[i]);
        vec3 right = vec3(cos(angleRad), sin(angleRad), 0.0);
        vec3 up = vec3(0.0, 0.0, 1.0);
        
        // ✅ 바닥(center) + 절반 높이
        vec3 planeCenter = center + vec3(0, 0, height * 0.5);
        
        EmitQuad(planeCenter, right, up, width, height, i);
    }
    
}
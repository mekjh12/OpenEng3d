#version 450 core

layout(points) in;
layout(triangle_strip, max_vertices = 12) out;  // 3 quads × 4 vertices

// Vertex Shader 출력
in VS_OUT {
    vec3 worldPos;
    vec3 color;
    float size;
    mat4 transform;
} gs_in[];

// Fragment Shader로 전달
out vec2 fTexCoord;      // UV 좌표
out flat int fPlaneIndex;  // 평면 인덱스
out vec3 vColor;         // 디버그용 색상
out vec3 vViewPos;

// Uniform
uniform mat4 vp;
uniform mat4 view;

// ===== Transform 행렬에서 Z축 회전 추출 =====
float GetZRotation(mat4 transform)
{
    // X축 벡터의 XY 성분에서 회전 각도 추출 (라디안)
    // transform[0] = X축 방향 벡터
    return atan(transform[0].y, transform[0].x);
}

// ===== Atlas UV 계산 함수 =====
vec2 GetAtlasOffset(int planeIndex)
{
    // 3개의 수직 평면, 각각 0.25 너비
    if (planeIndex < 3)
    {
        return vec2(planeIndex * 0.25, 0.0);
    }
    return vec2(0.0, 0.0);
}

vec2 GetAtlasSize(int planeIndex)
{
    return vec2(0.25, 1.0);
}

// ===== 크로스 빌보드용 헬퍼 함수 =====
void EmitQuad(vec3 center, vec3 right, vec3 up, float size, vec3 color, int planeIndex)
{
    vec3 halfRight = right * size;
    vec3 halfUp = up * size;
    
    vec2 uvOffset = GetAtlasOffset(planeIndex);
    vec2 uvSize = GetAtlasSize(planeIndex);
    
    // 좌하 (Left-Bottom)
    vec3 worldPos0 = center - halfRight;
    vViewPos = (view * vec4(worldPos0, 1.0)).xyz;
    vColor = color;
    fTexCoord = uvOffset;
    fPlaneIndex = planeIndex;
    gl_Position = vp * vec4(worldPos0, 1.0);
    EmitVertex();
    
    // 우하 (Right-Bottom)
    vec3 worldPos1 = center + halfRight;
    vViewPos = (view * vec4(worldPos1, 1.0)).xyz;
    vColor = color;
    fTexCoord = uvOffset + vec2(uvSize.x, 0.0);
    fPlaneIndex = planeIndex;
    gl_Position = vp * vec4(worldPos1, 1.0);
    EmitVertex();
    
    // 좌상 (Left-Top)
    vec3 worldPos2 = center - halfRight + 2.0 * halfUp;
    vViewPos = (view * vec4(worldPos2, 1.0)).xyz;
    vColor = color;
    fTexCoord = uvOffset + vec2(0.0, uvSize.y);
    fPlaneIndex = planeIndex;
    gl_Position = vp * vec4(worldPos2, 1.0);
    EmitVertex();
    
    // 우상 (Right-Top)
    vec3 worldPos3 = center + halfRight + 2.0 * halfUp;
    vViewPos = (view * vec4(worldPos3, 1.0)).xyz;
    vColor = color;
    fTexCoord = uvOffset + uvSize;
    fPlaneIndex = planeIndex;
    gl_Position = vp * vec4(worldPos3, 1.0);
    EmitVertex();
    
    EndPrimitive();
}

void main() 
{
    vec3 worldPos = gs_in[0].worldPos;
    vec3 color = gs_in[0].color;
    float size = gs_in[0].size;
    mat4 transform = gs_in[0].transform;
    
    // ✅ Transform에서 Z축 회전 각도 추출 (라디안)
    float baseRotation = GetZRotation(transform);
    
    // ===== 3개의 수직 평면 생성 (60도 간격) =====
    float angles[3] = float[3](0.0, 60.0, 120.0);
    
    // Up 벡터 (Z축 방향, 월드 업)
    vec3 up = vec3(0.0, 0.0, 1.0);
    
    for (int i = 0; i < 3; i++)
    {
        // ✅ 기본 각도(도) + Transform의 회전(라디안)
        float angleRad = radians(angles[i]) - baseRotation; // baseRotation이 플러스, 마이너스인지 아직 미해결
        
        // Right 벡터 (XY 평면에서 회전)
        vec3 right = vec3(cos(angleRad), sin(angleRad), 0.0);
        
        // 각 평면 생성
        EmitQuad(worldPos, right, up, size, color, i);
    }
}
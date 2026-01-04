#version 450 core

layout(points) in;
layout(triangle_strip, max_vertices = 12) out;  // 3 quads * 4 vertices
layout(std140, binding = 0) uniform CameraBlock {mat4 view; mat4 proj; mat4 vp;} camera;

// Vertex Shader 출력
in VS_OUT {
    vec3 worldPos;
    vec3 color;
    float horizontalSize;
    float verticalSize;
    mat4 transform;
} gs_in[];

// Fragment Shader로 전달
out vec2 fTexCoord;
out flat int fPlaneIndex;
out vec3 vNormal;
out vec3 vViewPos;
out vec3 vColor;

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

float GetZRotation(mat4 transform)
{
    return atan(transform[0].y, transform[0].x);
}

// ✅ 가로/세로 크기 독립적으로 받기
void EmitQuad(vec3 center, vec3 right, vec3 up, float horizontalSize, float verticalSize, 
              vec3 color, int planeIndex, vec3 planeNormal)
{
    vec3 halfRight = right * horizontalSize;
    vec3 halfUp = up * verticalSize;
    
    vec2 uvOffset = GetAtlasOffset(planeIndex);
    vec2 uvSize = GetAtlasSize(planeIndex);
    
    // 좌하 (Left-Bottom)
    vec3 worldPos0 = center - halfRight;
    vViewPos = (camera.view * vec4(worldPos0, 1.0)).xyz;
    vColor = color;
    vNormal = planeNormal;
    fTexCoord = uvOffset;
    fPlaneIndex = planeIndex;
    gl_Position = camera.vp * vec4(worldPos0, 1.0);
    EmitVertex();
    
    // 우하 (Right-Bottom)
    vec3 worldPos1 = center + halfRight;
    vViewPos = (camera.view * vec4(worldPos1, 1.0)).xyz;
    vColor = color;
    vNormal = planeNormal;
    fTexCoord = uvOffset + vec2(uvSize.x, 0.0);
    fPlaneIndex = planeIndex;
    gl_Position = camera.vp * vec4(worldPos1, 1.0);
    EmitVertex();
    
    // 좌상 (Left-Top)
    vec3 worldPos2 = center - halfRight + 2.0 * halfUp;
    vViewPos = (camera.view * vec4(worldPos2, 1.0)).xyz;
    vColor = color;
    vNormal = planeNormal;
    fTexCoord = uvOffset + vec2(0.0, uvSize.y);
    fPlaneIndex = planeIndex;
    gl_Position = camera.vp * vec4(worldPos2, 1.0);
    EmitVertex();
    
    // 우상 (Right-Top)
    vec3 worldPos3 = center + halfRight + 2.0 * halfUp;
    vViewPos = (camera.view * vec4(worldPos3, 1.0)).xyz;
    vColor = color;
    vNormal = planeNormal;
    fTexCoord = uvOffset + uvSize;
    fPlaneIndex = planeIndex;
    gl_Position = camera.vp * vec4(worldPos3, 1.0);
    EmitVertex();
    
    EndPrimitive();
}

void main() 
{
    // 입력 데이터 가져오기
    vec3 worldPos = gs_in[0].worldPos;
    vec3 color = gs_in[0].color;
    float horizontalSize = gs_in[0].horizontalSize;
    float verticalSize = gs_in[0].verticalSize;
    mat4 transform = gs_in[0].transform;
    
    // 각 평면에 대해 쿼드 생성
    float baseRotation = GetZRotation(transform);
    float angles[3] = float[3](0.0, 60.0, 120.0);
    vec3 up = vec3(0.0, 0.0, 1.0);
    
    for (int i = 0; i < 3; i++)
    {
        float angleRad = radians(angles[i] + 90) + baseRotation;
        vec3 right = vec3(cos(angleRad), sin(angleRad), 0.0);        
        vec3 planeNormal = normalize(cross(up, right));
        EmitQuad(worldPos, right, up, horizontalSize, verticalSize, color, i, planeNormal);
    }
}
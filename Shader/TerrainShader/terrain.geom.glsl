//-----------------------------------------------------------------------------
// Geometry Shader - 실제 삼각형으로 정확한 법선 계산
//-----------------------------------------------------------------------------
#version 430

layout(triangles) in;
layout(triangle_strip, max_vertices = 3) out;

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp; 
    vec4 cameraPos;
} camera;

// TES로부터 입력
in vec2 Tex3[];
in float Height[];
in vec4 fragPos[];

// Fragment Shader로 출력
out vec2 frag_Tex3;
out float frag_Height;
out vec4 frag_fragPos;
out vec4 frag_viewPos;
flat out vec3 frag_Normal;  // ⭐ flat: 삼각형당 하나의 법선

void main()
{
    // 삼각형의 세 정점 (월드 공간)
    vec3 v0 = fragPos[0].xyz;
    vec3 v1 = fragPos[1].xyz;
    vec3 v2 = fragPos[2].xyz;
    
    // 두 변 벡터
    vec3 edge1 = v1 - v0;
    vec3 edge2 = v2 - v0;
    
    // 외적으로 법선 계산 (완벽하게 삼각형에 수직)
    vec3 normal = normalize(cross(edge1, edge2));
    
    // 세 정점 모두 같은 법선 사용 (Flat Shading)
    for (int i = 0; i < 3; i++)
    {
        gl_Position = gl_in[i].gl_Position;
        
        frag_Tex3 = Tex3[i];
        frag_Height = Height[i];
        frag_fragPos = fragPos[i];
        frag_viewPos = camera.view * fragPos[i];
        frag_Normal = normal;
        
        EmitVertex();
    }
    
    EndPrimitive();
}
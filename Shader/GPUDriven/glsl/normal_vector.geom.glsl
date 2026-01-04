#version 450 core
layout(triangles) in;
layout(line_strip, max_vertices = 6) out;

uniform mat4 vp;
uniform float normalLength;

in VS_OUT {
    vec3 worldPos;
    vec3 worldNormal;
} gs_in[];

out vec3 vColor;

void main() 
{
    // 각 정점마다 법선 벡터 그리기
    for(int i = 0; i < 3; i++)
    {
        vec3 startPos = gs_in[i].worldPos;
        vec3 normal = gs_in[i].worldNormal;
        vec3 endPos = startPos + normal * normalLength;
        
        // 노란색으로 법선 벡터 표시
        vColor = normal * 0.5f + vec3(1) * 0.5;
        
        // 시작점
        gl_Position = vp * vec4(startPos, 1.0);
        EmitVertex();
        
        // 끝점
        gl_Position = vp * vec4(endPos, 1.0);
        EmitVertex();
        
        EndPrimitive();
    }
}
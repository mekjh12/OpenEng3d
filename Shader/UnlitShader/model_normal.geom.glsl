#version 450 core
layout(triangles) in;
layout(line_strip, max_vertices = 12) out;

in VS_OUT {
    vec3 viewPos;      // ✅ View Space
    vec3 viewNormal;   // ✅ View Space
} gs_in[];

uniform mat4 projection;  // ✅ mvp 대신 projection 사용
uniform float normalLength;

out vec3 vColor;

void main() 
{
    // 앞면 법선 (노란색)
    for(int i = 0; i < 3; i++)
    {
        vec3 viewPos = gs_in[i].viewPos;
        vec3 viewNormal = gs_in[i].viewNormal;
        
        vColor = vec3(1.0, 0.0, 0.0);
        
        // ✅ View Space → Clip Space (Projection만 적용)
        gl_Position = projection * vec4(viewPos, 1.0);
        EmitVertex();
        
        vec3 normalEnd = viewPos + viewNormal * normalLength;
        gl_Position = projection * vec4(normalEnd, 1.0);
        EmitVertex();
        
        EndPrimitive();
    }
    
    // 뒷면 법선 (시안색)
    for(int i = 0; i < 3; i++)
    {
        vec3 viewPos = gs_in[i].viewPos;
        vec3 viewNormal = -gs_in[i].viewNormal;
        
        vColor = vec3(0.0, 0.0, 1.0);
        
        gl_Position = projection * vec4(viewPos, 1.0);
        EmitVertex();
        
        vec3 normalEnd = viewPos + viewNormal * normalLength * 0.5f;
        gl_Position = projection * vec4(normalEnd, 1.0);
        EmitVertex();
        
        EndPrimitive();
    }
}
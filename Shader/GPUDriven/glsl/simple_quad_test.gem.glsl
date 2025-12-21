#version 450 core
layout(points) in;
layout(triangle_strip, max_vertices = 4) out;

uniform mat4 vp;
uniform float quadSize;
uniform vec3 u_cameraPosition;

in VS_OUT {
    vec3 worldPos;
    vec3 color;
} gs_in[];

out vec3 vColor;

void main() 
{
    vec3 worldPos = gs_in[0].worldPos;
    vec3 color = gs_in[0].color;
    float size = quadSize;
    
    // ===== 빌보드 벡터 계산 =====
    // 카메라를 향하는 방향
    vec3 forward = normalize(u_cameraPosition - worldPos);
    
    // 월드 Up 벡터 (Z축)  ✅ 수정
    vec3 worldUp = vec3(0.0, 0.0, 1.0);
    
    // Right 벡터 (카메라 기준 오른쪽)
    vec3 right = normalize(cross(worldUp, forward));
    
    // Up 벡터 (카메라 기준 위쪽)
    vec3 up = cross(forward, right);
    
    // ===== Triangle Strip으로 쿼드 생성 (반시계 방향) =====
    
    // 좌하 (Left-Bottom)
    vColor = color;
    gl_Position = vp * vec4(worldPos - right * size - up * size, 1.0);
    EmitVertex();
    
    // 우하 (Right-Bottom)
    vColor = color;
    gl_Position = vp * vec4(worldPos + right * size - up * size, 1.0);
    EmitVertex();
    
    // 좌상 (Left-Top)
    vColor = color;
    gl_Position = vp * vec4(worldPos - right * size + up * size, 1.0);
    EmitVertex();
    
    // 우상 (Right-Top)
    vColor = color;
    gl_Position = vp * vec4(worldPos + right * size + up * size, 1.0);
    EmitVertex();
    
    EndPrimitive();
}
#version 420 core

layout(points) in;                                                              
layout(triangle_strip, max_vertices = 4) out;                                      

// 유니폼 변수
uniform mat4 vp;
uniform vec3 gCameraPos;
uniform float treeSize = 10.5f; // 나무 크기 조절

// 출력 변수 (프래그먼트 쉐이더로 전달)
out vec2 TexCoord;
out vec4 FragPos;

void main()                                                                         
{                                                                                   
    vec3 Pos = gl_in[0].gl_Position.xyz; // 나무의 월드 좌표
    vec3 toCamera = normalize(gCameraPos - Pos);
    vec3 up = vec3(0.0, 0.0, 1.0);
    vec3 right = cross(toCamera, up) * treeSize; // 카메라를 향하는 방향 계산

    // 사각형의 빌보드 생성
    FragPos = vec4(Pos + right, 1.0);
    gl_Position = vp * FragPos;
    TexCoord = vec2(1.0, 1.0);
    EmitVertex();

    FragPos = vec4(Pos - right, 1.0);
    gl_Position = vp* FragPos;
    TexCoord = vec2(0.0, 1.0);
    EmitVertex();

    FragPos = vec4(Pos + right + vec3(0, 0, treeSize * 2), 1.0);
    gl_Position = vp * FragPos;
    TexCoord = vec2(1.0, 0.0);
    EmitVertex();

    FragPos = vec4(Pos - right + vec3(0, 0, treeSize * 2), 1.0);
    gl_Position = vp * FragPos;
    TexCoord = vec2(0.0, 0.0);
    EmitVertex();
                                                                                    
    EndPrimitive();
}

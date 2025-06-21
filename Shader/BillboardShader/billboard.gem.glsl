#version 420
                                                                                    
layout(points) in;                                                                  
layout(triangle_strip) out;                                                         
layout(max_vertices = 4) out;                                                       
                                                                                    
uniform mat4 view;
uniform mat4 proj;
uniform vec3 gCameraPos;
                                                                                    
out vec2 TexCoord;
out vec4 FragPos;
                                                                                    
void main()                                                                         
{                                                                                   
    vec3 Pos = gl_in[0].gl_Position.xyz;
    vec3 toCamera = normalize(gCameraPos - Pos);
    vec3 up = vec3(0.0, 0.0, 1.0);
    vec3 right = cross(toCamera, up);

    // Atlas Index를 지정한다. 가로를 4개로 나눈다.
    int atlasIndex = int(Pos.z + Pos.x + Pos.y) % 4;
    float uCoord = 0.25 * atlasIndex;

    // 사각형의 빌보드를 만든다.
    FragPos = vec4(Pos + right * 10.5f, 1.0);
    gl_Position = proj * view * FragPos;
    TexCoord = vec2(uCoord + 0.25f, 1.0);
    EmitVertex();

    FragPos = vec4(Pos - right * 10.5f, 1.0);
    gl_Position = proj * view * FragPos;
    TexCoord = vec2(uCoord, 1.0);
    EmitVertex();
            

    FragPos = vec4(Pos + right * 10.5f + vec3(0, 0, 20), 1.0);
    gl_Position = proj * view * FragPos;
    TexCoord = vec2(uCoord + 0.25f, 0.0);
    EmitVertex();

    FragPos = vec4(Pos - right * 10.5f + vec3(0, 0, 20), 1.0);
    gl_Position = proj * view * FragPos;
    TexCoord = vec2(uCoord, 0.0);
    EmitVertex();
                                                                                    
    EndPrimitive();
}  
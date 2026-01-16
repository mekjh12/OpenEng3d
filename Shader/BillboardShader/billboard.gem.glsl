#version 420

layout(points) in;
layout(triangle_strip) out;
layout(max_vertices = 4) out;

uniform mat4 view;
uniform mat4 proj;
uniform vec3 gCameraPos;
uniform int atlasIndex;  // ✅ 외부에서 제어

out vec2 TexCoord;
out vec4 FragPos;

void main()
{
    vec3 Pos = gl_in[0].gl_Position.xyz;
    vec3 toCamera = normalize(gCameraPos - Pos);
    vec3 up = vec3(0.0, 0.0, 1.0);
    vec3 right = cross(toCamera, up);
    
    // ✅ atlasIndex를 uniform으로 받음 (기본값 0 = 아틀라스 없음)
    float uCoord = 0.25 * atlasIndex;
    
    // ✅ 또는 아틀라스를 완전히 제거
    // uCoord = 0.0;  // 전체 텍스처 사용
    
    float halfWidth = 10.5f;
    float height = 20.0f;
    
    // 좌하
    FragPos = vec4(Pos - right * halfWidth, 1.0);
    gl_Position = proj * view * FragPos;
    TexCoord = vec2(uCoord, 1.0);
    EmitVertex();
    
    // 우하
    FragPos = vec4(Pos + right * halfWidth, 1.0);
    gl_Position = proj * view * FragPos;
    TexCoord = vec2(uCoord + 0.25f, 1.0);
    EmitVertex();
    
    // 좌상
    FragPos = vec4(Pos - right * halfWidth + vec3(0, 0, height), 1.0);
    gl_Position = proj * view * FragPos;
    TexCoord = vec2(uCoord, 0.0);
    EmitVertex();
    
    // 우상
    FragPos = vec4(Pos + right * halfWidth + vec3(0, 0, height), 1.0);
    gl_Position = proj * view * FragPos;
    TexCoord = vec2(uCoord + 0.25f, 0.0);
    EmitVertex();
    
    EndPrimitive();
}
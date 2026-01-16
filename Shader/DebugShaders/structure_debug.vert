#version 430 core

// Fullscreen quad용 버텍스 셰이더
// 정점 데이터 없이 gl_VertexID만으로 fullscreen quad 생성

out vec2 TexCoord;

void main()
{
    // gl_VertexID를 이용한 fullscreen quad 생성
    // 0: (-1, -1), 1: (1, -1), 2: (-1, 1)
    // 3: (-1, 1),  4: (1, -1), 5: (1, 1)
    
    float x = -1.0 + float((gl_VertexID & 1) << 2);
    float y = -1.0 + float((gl_VertexID & 2) << 1);
    
    TexCoord = (vec2(x, y) + 1.0) * 0.5;
    gl_Position = vec4(x, y, 0.0, 1.0);
}
#version 450 core

// 풀스크린 삼각형을 위한 버텍스 셰이더
// 버텍스 데이터 없이 gl_VertexID만 사용하여 풀스크린 삼각형 생성

out vec2 vTexCoord;

void main()
{
    // gl_VertexID를 사용하여 풀스크린 삼각형 정점 생성
    // 0: 좌하, 1: 우하(화면밖), 2: 좌상(화면밖)
    vec2 positions[3] = vec2[](
        vec2(-1.0, -1.0),
        vec2( 3.0, -1.0),
        vec2(-1.0,  3.0)
    );
    
    vec2 texCoords[3] = vec2[](
        vec2(0.0, 0.0),
        vec2(2.0, 0.0),
        vec2(0.0, 2.0)
    );
    
    gl_Position = vec4(positions[gl_VertexID], 0.0, 1.0);
    vTexCoord = texCoords[gl_VertexID];
}
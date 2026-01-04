//-----------------------------------------------------------------------------
// Geometry Shader - 삼각형 법선을 계산하고 RGB 라인으로 시각화
// Vertex 0: 빨강, Vertex 1: 녹색, Vertex 2: 파랑
//-----------------------------------------------------------------------------
#version 430

layout(triangles) in;
layout(line_strip, max_vertices = 6) out;  // 3개 라인 × 2개 정점

layout(std140, binding = 0) uniform CameraBlock {
    mat4 view; 
    mat4 proj; 
    mat4 vp; 
    vec4 cameraPos;
} camera;

// TES로부터 입력
in vec2 Tex3[];
in float Height[];
in vec4 fragPos[];    // 월드 위치

// Fragment Shader로 출력
out vec3 lineColor;

uniform float normalLength = 5.0;

void main()
{
    // 삼각형의 세 정점 (월드 공간)
    vec3 v0 = fragPos[0].xyz;
    vec3 v1 = fragPos[1].xyz;
    vec3 v2 = fragPos[2].xyz;
    
    // 두 변 벡터
    vec3 edge1 = v1 - v0;
    vec3 edge2 = v2 - v0;
    
    // 외적으로 삼각형 법선 계산
    vec3 normal = normalize(cross(edge1, edge2));

    // 각 꼭짓점의 색상
    vec3 colors[3] = vec3[3](
        vec3(1.0, 0.0, 0.0),  // 빨강 (Vertex 0)
        vec3(0.0, 1.0, 0.0),  // 녹색 (Vertex 1)
        vec3(0.0, 0.0, 1.0)   // 파랑 (Vertex 2)
    );
    
    // 각 꼭짓점마다 법선 라인 생성
    for (int i = 0; i < 3; i++)
    {
        // 라인 시작점 (정점 위치)
        vec4 startPos = fragPos[i];
        if (i==1) startPos += vec4(0.0, 0.05, 0.0, 0.0);
        if (i==2) startPos += vec4(0.05, 0.0, 0.0, 0.0);

        vec4 clipStart = camera.vp * startPos;
        
        // 라인 끝점 (법선 방향으로 normalLength만큼)
        float nLength = normalLength;
        if (i==0) nLength *= 1.0f;
        if (i==1) nLength *= 0.75f;
		if (i==2) nLength *= 0.5f;

        vec4 endPos = vec4(fragPos[i].xyz + normal * nLength, 1.0);
        vec4 clipEnd = camera.vp * endPos;
        
        // 라인 시작점 출력
        gl_Position = clipStart;
        lineColor = colors[i];
        EmitVertex();
        
        // 라인 끝점 출력
        gl_Position = clipEnd;
        lineColor = colors[i];
        EmitVertex(); 
        
        EndPrimitive();  // 각 라인 완성
    }
}
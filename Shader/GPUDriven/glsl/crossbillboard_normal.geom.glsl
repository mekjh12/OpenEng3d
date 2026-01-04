#version 450 core
layout(points) in;
layout(line_strip, max_vertices = 24) out;  // ✅ 3개 평면 × 4개 꼭지점 × 2개 정점 = 24

in VS_OUT {
    vec3 worldPos;
    mat4 transform;
    float size;
} gs_in[];

layout(std140, binding = 0) uniform CameraBlock {mat4 view; mat4 proj; mat4 vp;} camera;
uniform float normalLength;

out vec3 vColor;

// Transform 행렬에서 Z축 회전 추출
float GetZRotation(mat4 transform)
{
    return atan(transform[0].y, transform[0].x);
}

void main() 
{
    vec3 worldPos = gs_in[0].worldPos;
    mat4 transform = gs_in[0].transform;
    float size = gs_in[0].size;
    
    float baseRotation = GetZRotation(transform);
    
    // 3개의 수직 평면 (60도 간격)
    float angles[3] = float[3](0.0, 60.0, 120.0);
    vec3 up = vec3(0.0, 0.0, 1.0);
    
    for (int i = 0; i < 3; i++)
    {
        float angleRad = radians(angles[i]) - baseRotation;
        vec3 right = vec3(cos(angleRad), sin(angleRad), 0.0);
        
        // ✅ 평면 노멀 = right 벡터 (평면과 수직인 방향)
        vec3 planeNormal = right;
        
        // 법선 벡터 색상 (각 평면마다 다른 색상)
        if (i == 0) vColor = vec3(1.0, 0.0, 0.0);      // 빨강
        else if (i == 1) vColor = vec3(0.0, 1.0, 0.0); // 초록
        else vColor = vec3(0.0, 0.0, 1.0);             // 파랑
        
        // ✅ 평면의 4개 꼭지점 계산 (크로스 빌보드와 동일한 로직)
        vec3 halfRight = right * size;
        vec3 halfUp = up * size;
        
        // 4개 꼭지점 위치
        vec3 vertices[4];
        vertices[0] = worldPos - halfRight;                     // 좌하
        vertices[1] = worldPos + halfRight;                     // 우하
        vertices[2] = worldPos - halfRight + 2.0 * halfUp;      // 좌상
        vertices[3] = worldPos + halfRight + 2.0 * halfUp;      // 우상
        
        // ✅ 각 꼭지점에서 법선 벡터 그리기
        for (int j = 0; j < 4; j++)
        {
            // 법선 벡터 시작점 (꼭지점)
            gl_Position = camera.vp * vec4(vertices[j], 1.0);
            EmitVertex();
            
            // 법선 벡터 끝점
            gl_Position = camera.vp * vec4(vertices[j] + planeNormal * normalLength, 1.0);
            EmitVertex();
            
            EndPrimitive();
        }
    }
}
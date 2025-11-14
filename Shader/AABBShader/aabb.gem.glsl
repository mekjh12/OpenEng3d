#version 430

// 입력 프리미티브 타입 지정 (점)
layout (points) in;
// 출력 프리미티브 타입 지정 (삼각형 스트립, 직육면체는 36개 정점 필요)
layout (triangle_strip, max_vertices = 36) out;

// 버텍스 셰이더에서 받은 데이터
in VS_OUT {
    vec3 halfSize;
    vec4 color;
} gs_in[];

// 프래그먼트 셰이더로 전달할 데이터
out GS_OUT {
    vec4 color;
    vec3 normal;
    vec3 worldPos;
} gs_out;

// Uniform 변수들
uniform mat4 vp;                // View-Projection 행렬

// 직육면체의 8개 코너 계산 (Z-up 좌표계)
vec3 getCorner(vec3 center, vec3 halfSize, int index)
{
    vec3 corner = center;
    corner.x += ((index & 1) != 0) ? halfSize.x : -halfSize.x;
    corner.y += ((index & 2) != 0) ? halfSize.y : -halfSize.y;
    corner.z += ((index & 4) != 0) ? halfSize.z : -halfSize.z;
    return corner;
}

// 정점 출력 헬퍼 함수
void emitVertex(vec3 position, vec3 normal, vec4 color)
{
    gs_out.worldPos = position;
    gs_out.normal = normal;
    gs_out.color = color;
    gl_Position = vp * vec4(position, 1.0);
    EmitVertex();
}

// 사각형 면 생성 (CCW winding order for outward facing)
void emitQuad(vec3 v0, vec3 v1, vec3 v2, vec3 v3, vec3 normal, vec4 color)
{
    // 첫 번째 삼각형 (v0, v1, v2)
    emitVertex(v0, normal, color);
    emitVertex(v1, normal, color);
    emitVertex(v2, normal, color);
    EndPrimitive();
    
    // 두 번째 삼각형 (v2, v1, v3)
    emitVertex(v2, normal, color);
    emitVertex(v1, normal, color);
    emitVertex(v3, normal, color);
    EndPrimitive();
}

void main()
{
    // AABB 중심점과 속성 가져오기
    vec3 center = gl_in[0].gl_Position.xyz;
    vec3 halfSize = gs_in[0].halfSize;
    vec4 color = gs_in[0].color;
    
    // 8개의 코너 계산
    // 인덱스 비트: [z][y][x]
    // 0: [0][0][0] = (-x, -y, -z) 좌하앞
    // 1: [0][0][1] = (+x, -y, -z) 우하앞
    // 2: [0][1][0] = (-x, +y, -z) 좌상앞
    // 3: [0][1][1] = (+x, +y, -z) 우상앞
    // 4: [1][0][0] = (-x, -y, +z) 좌하뒤
    // 5: [1][0][1] = (+x, -y, +z) 우하뒤
    // 6: [1][1][0] = (-x, +y, +z) 좌상뒤
    // 7: [1][1][1] = (+x, +y, +z) 우상뒤
    vec3 corners[8];
    for (int i = 0; i < 8; i++)
    {
        corners[i] = getCorner(center, halfSize, i);
    }
    
    // 6개의 면 생성 (Z-up 좌표계, 외부를 향하는 면)
    
    // 윗면 (Z+) - 위를 향함
    // corners[4,5,6,7]의 순서를 CCW로
    emitQuad(corners[4], corners[5], corners[6], corners[7], vec3(0, 0, 1), color);
    
    // 아랫면 (Z-) - 아래를 향함
    // corners[0,1,2,3]의 순서를 CCW로 (아래에서 보면)
    emitQuad(corners[0], corners[2], corners[1], corners[3], vec3(0, 0, -1), color);
    
    // 앞면 (Y-) - 앞을 향함
    // corners[0,1,4,5]
    emitQuad(corners[0], corners[1], corners[4], corners[5], vec3(0, -1, 0), color);
    
    // 뒷면 (Y+) - 뒤를 향함
    // corners[2,3,6,7]
    emitQuad(corners[3], corners[2], corners[7], corners[6], vec3(0, 1, 0), color);
    
    // 왼쪽면 (X-) - 왼쪽을 향함
    // corners[0,2,4,6]
    emitQuad(corners[2], corners[0], corners[6], corners[4], vec3(-1, 0, 0), color);
    
    // 오른쪽면 (X+) - 오른쪽을 향함
    // corners[1,3,5,7]
    emitQuad(corners[1], corners[3], corners[5], corners[7], vec3(1, 0, 0), color);
}
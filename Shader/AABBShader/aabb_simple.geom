#version 430

layout (points) in;
layout (triangle_strip, max_vertices = 36) out;

// Uniforms: CPU에서 전달받는 데이터
uniform mat4 u_vp;      // View-Projection 행렬
uniform vec3 u_min;     // AABB 최소 지점 (Min Bounds)
uniform vec3 u_max;     // AABB 최대 지점 (Max Bounds)

// 코너 좌표 계산 함수 (Min, Max 조합)
vec3 getCorner(int index)
{
    vec3 corner;
    // 비트 연산으로 min/max 선택 (0: min, 1: max)
    corner.x = ((index & 1) != 0) ? u_max.x : u_min.x;
    corner.y = ((index & 2) != 0) ? u_max.y : u_min.y;
    corner.z = ((index & 4) != 0) ? u_max.z : u_min.z;
    return corner;
}

// 정점 변환 및 방출
void emitVertex(vec3 pos)
{
    gl_Position = u_vp * vec4(pos, 1.0);
    EmitVertex();
}

// 사각형 면 생성
void emitQuad(vec3 v0, vec3 v1, vec3 v2, vec3 v3)
{
    // 첫 번째 삼각형
    emitVertex(v0);
    emitVertex(v1);
    emitVertex(v2);
    EndPrimitive();
    
    // 두 번째 삼각형
    emitVertex(v2);
    emitVertex(v1);
    emitVertex(v3);
    EndPrimitive();
}

void main()
{
    // 8개의 코너 계산
    // 0: (min.x, min.y, min.z)
    // 7: (max.x, max.y, max.z)
    vec3 corners[8];
    for (int i = 0; i < 8; i++)
    {
        corners[i] = getCorner(i);
    }
    
    // 6개의 면 그리기 (순서: CCW)
    
    // 윗면 (Z+)
    emitQuad(corners[4], corners[5], corners[6], corners[7]);
    
    // 아랫면 (Z-)
    emitQuad(corners[0], corners[2], corners[1], corners[3]);
    
    // 앞면 (Y-)
    emitQuad(corners[0], corners[1], corners[4], corners[5]);
    
    // 뒷면 (Y+)
    emitQuad(corners[3], corners[2], corners[7], corners[6]);
    
    // 왼쪽면 (X-)
    emitQuad(corners[2], corners[0], corners[6], corners[4]);
    
    // 오른쪽면 (X+)
    emitQuad(corners[1], corners[3], corners[5], corners[7]);
}
#version 430 core

layout(points) in;
layout(triangle_strip, max_vertices = 36) out;  // 큐브는 최대 36개 정점 (6면 * 2삼각형 * 3정점)

// AABB 구조체 정의
struct AABB
{
    vec3 min;
    float pad1;
    vec3 max;
    float pad2;
};

// SSBO: AABB 배열
layout(std430, binding = 0) readonly buffer AABBBuffer
{
    AABB aabbs[];
};

// 입력
in int vInstanceID[];

// 출력
out vec4 gViewPos;

// 유니폼
uniform mat4 view;
uniform mat4 proj;

// AABB의 8개 정점을 생성하는 함수
vec3 getAABBVertex(vec3 minPos, vec3 maxPos, int index)
{
    // 큐브의 8개 코너
    vec3 corners[8] = vec3[8](
        vec3(minPos.x, minPos.y, minPos.z),  // 0: 좌하전
        vec3(maxPos.x, minPos.y, minPos.z),  // 1: 우하전
        vec3(maxPos.x, maxPos.y, minPos.z),  // 2: 우상전
        vec3(minPos.x, maxPos.y, minPos.z),  // 3: 좌상전
        vec3(minPos.x, minPos.y, maxPos.z),  // 4: 좌하후
        vec3(maxPos.x, minPos.y, maxPos.z),  // 5: 우하후
        vec3(maxPos.x, maxPos.y, maxPos.z),  // 6: 우상후
        vec3(minPos.x, maxPos.y, maxPos.z)   // 7: 좌상후
    );
    return corners[index];
}

// 정점을 방출하는 헬퍼 함수
void emitVertex(vec3 worldPos)
{
    vec4 viewPos = view * vec4(worldPos, 1.0);
    gViewPos = viewPos;
    gl_Position = proj * viewPos;
    EmitVertex();
}

// 쿼드를 그리는 함수 (2개의 삼각형)
void emitQuad(vec3 v0, vec3 v1, vec3 v2, vec3 v3)
{
    // 첫 번째 삼각형
    emitVertex(v0);
    emitVertex(v1);
    emitVertex(v2);
    EndPrimitive();

    // 두 번째 삼각형
    emitVertex(v0);
    emitVertex(v2);
    emitVertex(v3);
    EndPrimitive();
}

void main()
{
    int aabbIndex = vInstanceID[0];
    
    // AABB 데이터 가져오기
    AABB aabb = aabbs[aabbIndex];
    vec3 minPos = aabb.min;
    vec3 maxPos = aabb.max;
    
    // AABB의 8개 정점 계산
    vec3 v[8];
    for (int i = 0; i < 8; i++)
    {
        v[i] = getAABBVertex(minPos, maxPos, i);
    }
    
    // 6개의 면을 렌더링
    // 전면 (Z-)
    emitQuad(v[0], v[1], v[2], v[3]);
    
    // 후면 (Z+)
    emitQuad(v[5], v[4], v[7], v[6]);
    
    // 좌면 (x-)
    emitQuad(v[4], v[0], v[3], v[7]);
    
    // 우면 (x+)
    emitQuad(v[1], v[5], v[6], v[2]);
    
    // 하면 (Y-)
    emitQuad(v[4], v[5], v[1], v[0]);
    
    // 상면 (Y+)
    emitQuad(v[3], v[2], v[6], v[7]);
}